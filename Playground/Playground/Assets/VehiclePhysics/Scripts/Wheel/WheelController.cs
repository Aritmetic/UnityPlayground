using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;

namespace NWH.WheelController3D
{
    [Serializable]
    public partial class WheelController : MonoBehaviour
    {
        [SerializeField]
        public Wheel wheel;

        [SerializeField, HideInInspector]
        public Spring spring;

        [SerializeField, HideInInspector]
        public Damper damper;

        /// <summary>
        /// Forward (longitudinal) friction info.
        /// </summary>
        [SerializeField, HideInInspector]
        public Friction fFriction;

        /// <summary>
        /// Side (lateral) friction info.
        /// </summary>
        [SerializeField, HideInInspector]
        public Friction sFriction;

        /// <summary>
        /// Array of rays and related data that are shot each frame to detect surface features.
        /// Contains offsets, hit points, normals, etc. of each point.
        /// </summary>
        [SerializeField]
        private WheelHit[] wheelHits;

        [SerializeField]
        private LayerMask scanIgnoreLayers = 1 << 20 | Physics.IgnoreRaycastLayer;

        /// <summary>
        /// Number of raycasts in the forward / longitudinal direction.
        /// </summary>
        [SerializeField]
        private int forwardScanResolution = 8; // resolution of the first scan pass

        /// <summary>
        /// Number of raycasts in the side / lateral direction.
        /// </summary>
        [SerializeField]
        private int sideToSideScanResolution = 3; // number of scan planes (side-to-side)

        /// <summary>
        /// True if wheel touching ground.
        /// </summary>
        [SerializeField]
        private bool hasHit = true;
        [SerializeField]
        private bool prevHasHit = true;

        // If set to true draws hit points and related data.
        public bool debug;

        /// <summary>
        /// Root object of the vehicle.
        /// </summary>
        [SerializeField]
        public GameObject parent;
        private Rigidbody parentRigidbody;

        /// <summary>
        /// If enabled mesh collider mimicking the shape of rim and wheel will be positioned so that wheel can not pass through objects in case raycast does not detect the surface in time.
        /// </summary>
        public bool useRimCollider = true;

        /// <summary>
        /// Side of the vehicle.
        /// </summary>
        public enum Side
        {
            Left = -1,
            Right = 1,
            Center = 0,
            Auto = 2
        }

        /// <summary>
        /// Side the wheel is on.
        /// </summary>
        [SerializeField]
        private Side vehicleSide = Side.Auto;

        /// <summary>
        /// Current active preset enum value.
        /// </summary>
        public FrictionPreset.FrictionPresetEnum activeFrictionPresetEnum;

        /// <summary>
        /// Current active friction preset.
        /// </summary>
        public FrictionPreset activeFrictionPreset;

        /// <summary>
        /// Contains point in which wheel touches ground. Not valid if !isGrounded.
        /// </summary>
        public WheelHit wheelHit = new WheelHit();

        public bool singleRay = false;
        public WheelHit singleWheelHit = new WheelHit();

        /// <summary>
        /// Enables some wheel behaviors specific to tracked vehicles, specifically the fact that there is no wheel spin.
        /// </summary>
        [HideInInspector]
        public bool trackedVehicle = false;

        /// <summary>
        /// Only for tracked vehicles that use NVP wheel enlargement.
        /// Determines how much larger the wheel radius virtually is.
        /// </summary>
        [HideInInspector]
        public float trackedOffset = 0;

        // Wheel rotation
        private Quaternion steerQuaternion;
        private Quaternion camberQuaternion;
        private Quaternion totalRotation;

        private float boundsX, boundsY, boundsZ, boundsW;
        private float stepX, stepY;
        private float rayLength;
        private int minDistRayIndex;

        // Weighted average
        private WheelHit wheelRay;
        private float n;
        private float minWeight = Mathf.Infinity;
        private float maxWeight = 0f;
        private float weightSum = 0f;
        private int validCount = 0;

        [NonSerialized]
        private Vector3 hitPointSum = Vector3.zero;
        [NonSerialized]
        private Vector3 normalSum = Vector3.zero;
        [NonSerialized]
        private Vector3 point = new Vector3();
        [NonSerialized]
        private Vector3 normal = new Vector3();
        private float weight = 0;

        private float forwardSum = 0;
        private float sideSum = 0;
        private float angleSum = 0;
        private float offsetSum = 0;

        private Vector3 transformUp;
        private Vector3 transformForward;
        private Vector3 transformRight;
        private Vector3 transformPosition;
        private Quaternion transformRotation;

        public bool applyForceToOthers = false;
        public float maxPutDownForce;

        private RaycastHit tmpRaycastHit;
        private Vector3 origin;
        private Vector3 alternateForwardNormal;
        private Vector3 totalForce;
        private Vector3 forcePoint;
        private Vector3 hitDir;
        private Vector3 predictedDistance;
        private Vector3 wheelDown;
        private Vector3 offsetPrecalc;
        private float prevForwardSpeed;
        private float prevFreeRollingAngularVelocity;

        private Vector3 projectedNormal;
        private Vector3 projectedAltNormal;

        private void Awake()
        {
            // Fill in necessary values and generate curves if needed
            Initialize();

            // Set the world position to the position of the wheel
            if (wheel.visual != null)
            {
                wheel.worldPosition = wheel.visual.transform.position;
                wheel.up = wheel.visual.transform.up;
                wheel.forward = wheel.visual.transform.forward;
                wheel.right = wheel.visual.transform.right;
            }

            if (wheel.nonRotating != null)
            {
                wheel.nonRotatingPostionOffset = transform.InverseTransformDirection(wheel.nonRotating.transform.position - wheel.visual.transform.position);
            }

            // Initialize the wheel params
            wheel.Initialize(this);

            InitializeScanParams();

            // Find parent
            parentRigidbody = parent.GetComponent<Rigidbody>();

            // Initialize spring length to starting value.
            spring.length = spring.maxLength * 0.5f;

            // Invert layers so that all the other layers are detected except for the ignore ones.
            scanIgnoreLayers = ~scanIgnoreLayers;
        }


        public void InitializeScanParams()
        {
            // Scan start point
            boundsX = -wheel.width / 2f;
            boundsY = -wheel.tireRadius;

            // Scan end point
            boundsZ = wheel.width / 2f + 0.000001f;
            boundsW = wheel.tireRadius + 0.000001f;

            // Increment
            stepX = sideToSideScanResolution == 1 ? 1 : (wheel.width) / (sideToSideScanResolution - 1);
            stepY = forwardScanResolution == 1 ? 1 : (wheel.tireRadius * 2f) / (forwardScanResolution - 1);

            // Initialize wheel rays
            wheelHits = new WheelHit[forwardScanResolution * sideToSideScanResolution];

            int w = 0;
            for (float x = boundsX; x <= boundsZ; x += stepX)
            {
                int h = 0;
                for (float y = boundsY; y <= boundsW; y += stepY)
                {
                    int index = w * forwardScanResolution + h;

                    WheelHit wr = new WheelHit();
                    wr.angleForward = Mathf.Asin(y / (wheel.tireRadius + 0.000001f));
                    wr.curvatureOffset = Mathf.Cos(wr.angleForward) * wheel.tireRadius;

                    float xOffset = x;
                    if (sideToSideScanResolution == 1) xOffset = 0;
                    wr.offset = new Vector2(xOffset, y);
                    wheelHits[index] = wr;

                    h++;
                }
                w++;
            }
        }


        public void FixedUpdate()
        {
            prevHasHit = hasHit;

            transformPosition = transform.position;
            transformRotation = transform.rotation;
            transformForward = transform.forward;
            transformRight = transform.right;
            transformUp = transform.up;

            if (!parentRigidbody.IsSleeping())
            {
                // Find contact point with ground
                HitUpdate();
                SuspensionUpdate();
                CalculateWheelDirectionsAndRotations();
                WheelUpdate();
                FrictionUpdate();
                UpdateForces();
            }
        }

        private void CalculateWheelDirectionsAndRotations()
        {
            steerQuaternion = Quaternion.AngleAxis(wheel.steerAngle, transformUp);
            camberQuaternion = Quaternion.AngleAxis(-(int)vehicleSide * wheel.camberAngle, transformForward);
            totalRotation = steerQuaternion * camberQuaternion;

            wheel.up = totalRotation * transformUp;
            wheel.forward = totalRotation * transformForward;
            wheel.right = totalRotation * transformRight;
            wheel.inside = wheel.right * -(int)vehicleSide;
        }

        /// <summary>
        /// Searches for wheel hit point by iterating WheelScan() function to the requested scan depth.
        /// </summary>
        private void HitUpdate()
        {
            // Hit flag     
            float minDistance = Mathf.Infinity;
            wheelDown = -wheel.up;

            float distanceThreshold = spring.maxLength - spring.length;
            rayLength = wheel.tireRadius * 2.1f + distanceThreshold;

            offsetPrecalc = transformPosition - transformUp * spring.length + wheel.up * wheel.tireRadius - wheel.inside * wheel.rimOffset;

            int validHitCount = 0;
            minDistRayIndex = -1;
            hasHit = false;

            if (singleRay)
            {
                singleWheelHit.valid = false;

                bool grounded = Physics.Raycast(offsetPrecalc, wheelDown, out singleWheelHit.raycastHit, rayLength + wheel.tireRadius, scanIgnoreLayers);

                if (grounded)
                {
                    float distanceFromTire = singleWheelHit.raycastHit.distance - wheel.tireRadius - wheel.tireRadius;
                    if (distanceFromTire > distanceThreshold) return;
                    singleWheelHit.valid = true;
                    hasHit = true;
                    singleWheelHit.distanceFromTire = distanceFromTire;

                    wheelHit.raycastHit = singleWheelHit.raycastHit;
                    wheelHit.Copy(singleWheelHit, false);
                    wheelHit.groundPoint = wheelHit.raycastHit.point;
                    wheelHit.raycastHit.point += wheel.up * wheel.tireRadius;
                    wheelHit.curvatureOffset = wheel.tireRadius;
                }
            }
            else
            {
                for (int i = 0; i < wheelHits.Length; i++)
                {
                    WheelHit wr = wheelHits[i];
                    wr.valid = false;

                    origin.x = wheel.forward.x * wr.offset.y + wheel.right.x * wr.offset.x + offsetPrecalc.x;
                    origin.y = wheel.forward.y * wr.offset.y + wheel.right.y * wr.offset.x + offsetPrecalc.y;
                    origin.z = wheel.forward.z * wr.offset.y + wheel.right.z * wr.offset.x + offsetPrecalc.z;
                    bool grounded = Physics.Raycast(origin, wheelDown, out tmpRaycastHit, rayLength + wr.curvatureOffset, scanIgnoreLayers);

                    if (grounded)
                    {
                        float distanceFromTire = tmpRaycastHit.distance - wr.curvatureOffset - wheel.tireRadius;

                        if (distanceFromTire > distanceThreshold) continue;

                        wr.valid = true;
                        wr.raycastHit = tmpRaycastHit;
                        wr.distanceFromTire = distanceFromTire;

                        validHitCount++;

                        if (distanceFromTire < minDistance)
                        {
                            minDistance = distanceFromTire;
                            minDistRayIndex = i;
                        }
                    }

                    wheelHits[i] = wr;
                }

                CalculateAverageWheelHit();
            }

            // Friction force directions
            if (hasHit)
            {
                wheelHit.forwardDir = Vector3.Normalize(Vector3.Cross(wheelHit.normal, -wheel.right));
                wheelHit.sidewaysDir = Quaternion.AngleAxis(90f, wheelHit.normal) * wheelHit.forwardDir;
            }
        }

        private void CalculateAverageWheelHit()
        {
            int count = 0;

            n = wheelHits.Length;

            minWeight = Mathf.Infinity;
            maxWeight = 0f;
            weightSum = 0f;
            validCount = 0;

            hitPointSum = Vector3.zero;
            normalSum = Vector3.zero;
            weight = 0;

            forwardSum = 0;
            sideSum = 0;
            angleSum = 0;
            offsetSum = 0;
            validCount = 0;

            for (int i = 0; i < n; i++)
            {
                wheelRay = wheelHits[i];
                if (wheelRay.valid)
                {
                    weight = wheel.tireRadius - wheelRay.distanceFromTire;
                    weight = weight * weight * weight * weight * weight;

                    if (weight < minWeight) minWeight = weight;
                    else if (weight > maxWeight) maxWeight = weight;

                    weightSum += weight;
                    validCount++;

                    normal = wheelRay.raycastHit.normal;
                    point = wheelRay.raycastHit.point;

                    hitPointSum.x += point.x * weight;
                    hitPointSum.y += point.y * weight;
                    hitPointSum.z += point.z * weight;

                    normalSum.x += normal.x * weight;
                    normalSum.y += normal.y * weight;
                    normalSum.z += normal.z * weight;

                    forwardSum += wheelRay.offset.y * weight;
                    sideSum += wheelRay.offset.x * weight;
                    angleSum += wheelRay.angleForward * weight;
                    offsetSum += wheelRay.curvatureOffset * weight;

                    count++;
                }
            }

            if (validCount == 0 || minDistRayIndex < 0)
            {
                hasHit = false;
                return;
            }

            wheelHit.raycastHit = wheelHits[minDistRayIndex].raycastHit;
            wheelHit.raycastHit.point = hitPointSum / weightSum;
            wheelHit.offset.y = forwardSum / weightSum;
            wheelHit.offset.x = sideSum / weightSum;
            wheelHit.angleForward = angleSum / weightSum;
            wheelHit.raycastHit.normal = Vector3.Normalize(normalSum / weightSum);
            wheelHit.curvatureOffset = offsetSum / weightSum;
            wheelHit.raycastHit.point += wheel.up * wheelHit.curvatureOffset;
            wheelHit.groundPoint = wheelHit.raycastHit.point - wheel.up * wheelHit.curvatureOffset;

            hasHit = true;
        }


        private void SuspensionUpdate()
        {
            spring.prevOverflow = spring.overflow;
            spring.overflow = 0f;
            if (hasHit && Vector3.Dot(wheelHit.raycastHit.normal, transformUp) > 0.1f)
            {
                spring.bottomedOut = spring.overExtended = false;

                // Calculate spring length from ground hit, position of the wheel and transform position.     
                if (singleRay)
                {
                    spring.targetPoint = wheelHit.raycastHit.point - wheel.right * wheel.rimOffset * (int)vehicleSide;
                }
                else
                {
                    spring.targetPoint = wheelHit.raycastHit.point
                         + wheel.up * wheel.tireRadius * 0.03f
                        - wheel.forward * wheelHit.offset.y
                        - wheel.right * wheelHit.offset.x
                        - wheel.right * wheel.rimOffset * (int)vehicleSide;
                }

                spring.length = -transform.InverseTransformPoint(spring.targetPoint).y;

                // If the spring is overcompressed remember the value for later force calculation and set spring to 0.
                // If the spring is overcompresset hit has not actually happened since the wheel is in the air.
                if (spring.length < 0f)
                {
                    spring.overflow = -spring.length;
                    spring.length = 0f;
                    spring.bottomedOut = true;
                }
                else if (spring.length > spring.maxLength)
                {
                    hasHit = false;
                    spring.length = spring.maxLength;
                    spring.overExtended = true;
                }
            }
            else
            {
                // If the wheel suddenly gets in the air smoothly extend it.
                spring.length = Mathf.Lerp(spring.length, spring.maxLength, Time.fixedDeltaTime * 8f);
            }

            spring.velocity = (spring.length - spring.prevLength) / Time.fixedDeltaTime;
            spring.compressionPercent = (spring.maxLength - spring.length) / spring.maxLength;
            spring.force = spring.maxForce * spring.forceCurve.Evaluate(spring.compressionPercent);

            // If spring has bottomed out add bottoming out force and if functioning normally add damper force.
            spring.overflowVelocity = 0f;
            if (spring.overflow > 0)
            {
                spring.overflowVelocity = (spring.overflow - spring.prevOverflow) / Time.fixedDeltaTime;
                spring.bottomOutForce = parentRigidbody.mass * -Physics.gravity.y * Mathf.Clamp(spring.overflowVelocity, 0f, Mathf.Infinity) * 0.015f;
                parentRigidbody.AddForceAtPosition(spring.bottomOutForce * transformUp, transformPosition, ForceMode.Impulse);
            }
            else
            {
                damper.maxForce = spring.length < spring.prevLength ? damper.unitBumpForce : damper.unitReboundForce;
                if (spring.length <= spring.prevLength)
                    damper.force = damper.unitBumpForce * damper.dampingCurve.Evaluate(Mathf.Abs(spring.velocity));
                else
                    damper.force = -damper.unitReboundForce * damper.dampingCurve.Evaluate(Mathf.Abs(spring.velocity));
            }

            spring.prevLength = spring.length;
        }


        private void WheelUpdate()
        {
            wheel.prevWorldPosition = wheel.worldPosition;
            wheel.worldPosition = transformPosition - transformUp * spring.length - wheel.inside * wheel.rimOffset;

            wheel.prevVelocity = wheel.velocity;
            wheel.velocity = parentRigidbody.GetPointVelocity(wheel.worldPosition);
            wheel.acceleration = (wheel.velocity - wheel.prevVelocity) / Time.fixedDeltaTime;

            // Calculate camber based on spring travel
            wheel.camberAngle = wheel.camberCurve.Evaluate(spring.length / spring.maxLength);

            // Tire load calculated from spring and damper force for wheelcollider compatibility
            wheel.tireLoad = Mathf.Clamp(spring.force + damper.force, 0.0f, Mathf.Infinity);
            if (hasHit) wheelHit.force = wheel.tireLoad;

            // Calculate visual rotation angle between 0 and 2PI radians.
            wheel.rotationAngle = (wheel.rotationAngle % 360.0f) + (wheel.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime);

            var axleRotation = Quaternion.AngleAxis(wheel.rotationAngle, transform.right);

            // Set rotation   
            wheel.worldRotation = totalRotation * axleRotation * transformRotation;

            // Apply rotation and position to visuals if assigned
            if (wheel.visual != null)
            {
                wheel.visual.transform.rotation = wheel.worldRotation;

                if (trackedVehicle)
                {
                    wheel.visual.transform.position = wheel.worldPosition - transform.up * trackedOffset;
                }
                else
                {
                    wheel.visual.transform.position = wheel.worldPosition;
                }

            }

            // Apply rotation and position to the non-rotationg objects if assigned
            if (wheel.nonRotating != null)
            {
                wheel.nonRotating.transform.rotation = totalRotation * transformRotation;

                if (trackedVehicle)
                {
                    wheel.nonRotating.transform.position = wheel.worldPosition + transform.TransformDirection(totalRotation * wheel.nonRotatingPostionOffset) - transform.up * trackedOffset;
                }
                else
                {
                    wheel.nonRotating.transform.position = wheel.worldPosition + transform.TransformDirection(totalRotation * wheel.nonRotatingPostionOffset);
                }
            }

            // Apply rotation to rim collider 
            if (useRimCollider)
            {
                wheel.rim.transform.position = wheel.worldPosition;
                wheel.rim.transform.rotation = steerQuaternion * camberQuaternion * transformRotation;
            }
        }


        /// <summary>
        /// Does lateral and longitudinal slip and force calculations.
        /// </summary>
        private void FrictionUpdate()
        {
            prevForwardSpeed = fFriction.speed;
            Vector3 contactVelocity = parentRigidbody.GetPointVelocity(wheelHit.raycastHit.point);

            if (hasHit)
            {
                fFriction.speed = Vector3.Dot(contactVelocity, wheelHit.forwardDir);
                sFriction.speed = Vector3.Dot(contactVelocity, wheelHit.sidewaysDir);
            }
            else
            {
                fFriction.speed = sFriction.speed = 0;
            }

            float lowerLimit = 3f - Mathf.Clamp(contactVelocity.magnitude, 0f, 3f);
            float wheelForwardSpeed = wheel.angularVelocity * wheel.tireRadius;
            float clampedWheelForwardSpeed = Mathf.Clamp(Mathf.Abs(wheelForwardSpeed), lowerLimit, Mathf.Infinity);

            //*******************
            // Side slip
            //*******************

            sFriction.slip = 0f;
            sFriction.force = 0f;

            if (hasHit)
            {
                if (trackedVehicle)
                    SetActiveFrictionPreset(FrictionPreset.Tracks);

                sFriction.slip = fFriction.speed == 0 ? 0 : (Mathf.Atan(sFriction.speed / clampedWheelForwardSpeed) * Mathf.Rad2Deg) / 80.0f;
                sFriction.force = Mathf.Sign(sFriction.slip) * activeFrictionPreset.Curve.Evaluate(Mathf.Abs(sFriction.slip)) * wheel.tireLoad * sFriction.forceCoefficient * 1.3f;
            }

            //*******************
            // Forward slip
            //*******************
            wheel.freeRollingAngularVelocity = fFriction.speed / wheel.tireRadius;

            float inertia = wheel.mass * wheel.tireRadius * wheel.tireRadius;
            float motorForce = wheel.motorTorque / wheel.tireRadius;
            float brakeForce = Mathf.Abs(wheel.brakeTorque / wheel.tireRadius);

            // Calculate wheel slip
            fFriction.slip = 0;
            if (hasHit)
            {
                float fClampedForwardSpeed = Mathf.Clamp(Mathf.Abs(fFriction.speed), 0.22f, Mathf.Infinity);
                fFriction.slip = fClampedForwardSpeed == 0 ? 0 : (((wheel.angularVelocity * wheel.tireRadius) - fFriction.speed) / fClampedForwardSpeed) * fFriction.slipCoefficient;
            }

            float clampedSlip = Mathf.Clamp(Mathf.Abs(fFriction.slip), 0.05f, Mathf.Infinity);

            // Calculate maximum force that wheel can put down before it starts to spin
            if (!trackedVehicle)
            {
                maxPutDownForce = activeFrictionPreset.Curve.Evaluate(clampedSlip) * wheel.tireLoad * fFriction.forceCoefficient * 1.3f;
            }
            else
            {
                maxPutDownForce = wheel.tireLoad * fFriction.forceCoefficient * 1.3f;
            }

            // Reduce residual angular velocity by the unused force
            float decelerationForce = Mathf.Sign(motorForce) * Mathf.Clamp(maxPutDownForce - Mathf.Abs(motorForce), 0f, Mathf.Infinity);
            float decelerationDelta = inertia == 0 ? 0 : ((decelerationForce * wheel.tireRadius) / inertia) * Time.fixedDeltaTime;

            // Increase residual angular velocity by the motor force that could not be put down
            float accelerationForce = Mathf.Sign(motorForce) * Mathf.Clamp((Mathf.Abs(motorForce) - maxPutDownForce), 0f, Mathf.Infinity);
            float accelerationDelta = inertia == 0 ? 0 : ((accelerationForce * wheel.tireRadius) / inertia) * Time.fixedDeltaTime;

            // Calculate residual angular velocity
            wheel.residualAngularVelocity += accelerationDelta - decelerationDelta;

            // Limit angular velocity so that brakes can slow down the rotation only until wheel is fully stopped.
            if (motorForce >= 0)
                wheel.residualAngularVelocity = Mathf.Clamp(wheel.residualAngularVelocity, 0f, Mathf.Infinity);
            else
                wheel.residualAngularVelocity = Mathf.Clamp(wheel.residualAngularVelocity, -Mathf.Infinity, 0f);

            // Continue spinning even after leaving groundW
            if (!hasHit && prevHasHit)
            {
                wheel.residualAngularVelocity = prevFreeRollingAngularVelocity;
            }
            wheel.angularVelocity = wheel.freeRollingAngularVelocity + wheel.residualAngularVelocity;

            // Calculate brakes
            float angularDeceleration = inertia == 0 ? 0 : -Mathf.Sign(wheel.angularVelocity) * ((brakeForce * wheel.tireRadius) / inertia) * Time.fixedDeltaTime;

            // Limit angular velocity after applying brakes so that brakes can slow down the rotation only until wheel is fully stopped.
            if (wheel.angularVelocity < 0)
                wheel.angularVelocity = Mathf.Clamp(wheel.angularVelocity + angularDeceleration, -Mathf.Infinity, 0f);
            else
                wheel.angularVelocity = Mathf.Clamp(wheel.angularVelocity + angularDeceleration, 0f, Mathf.Infinity);

            // Limit how much residual velocity a wheel can have. Too much will cause wheel to spin for long time after motor force is no longer applied.
            // Physically this would be more accurate but can be irritating (default wheelcollider does not limit this).
            wheel.residualAngularVelocity = Mathf.Sign(wheel.residualAngularVelocity) * Mathf.Clamp(Mathf.Abs(wheel.residualAngularVelocity), 0f, 1000f);

            // Make wheels free roll when slight braking is applied, but not enough to lock the wheel.
            if (hasHit && brakeForce != 0 && Mathf.Abs(motorForce) < brakeForce && brakeForce < maxPutDownForce)
                wheel.angularVelocity = wheel.freeRollingAngularVelocity;

            // No wheel spin for tracked vehicles
            if (trackedVehicle)
                wheel.angularVelocity = wheel.freeRollingAngularVelocity;

            // Calculate force that will be put down to the surface
            if (hasHit)
            {
                float smoothSpeed = fFriction.speed;
                if (contactVelocity.magnitude < 1f)
                {
                    smoothSpeed = Mathf.SmoothStep(prevForwardSpeed, fFriction.speed, Time.fixedDeltaTime * 2f);
                }
                fFriction.force = Mathf.Clamp(motorForce - Mathf.Sign(fFriction.speed) * Mathf.Clamp01(Mathf.Abs(smoothSpeed)) * brakeForce,
                    -maxPutDownForce, maxPutDownForce) * fFriction.forceCoefficient;
            }
            else
            {
                fFriction.force = 0f;
            }

            // Convert angular velocity to RPM
            wheel.rpm = wheel.angularVelocity * 9.55f;

            // Limit side and forward force if needed, useful for drift vehicles in arcade games
            if (fFriction.maxForce > 0)
            {
                fFriction.force = Mathf.Clamp(fFriction.force, -fFriction.maxForce, fFriction.maxForce);
            }
            if (sFriction.maxForce > 0)
            {
                sFriction.force = Mathf.Clamp(sFriction.force, -sFriction.maxForce, sFriction.maxForce);
            }

            // Fill in WheelHit info for Unity wheelcollider compatibility
            if (hasHit)
            {
                wheelHit.forwardSlip = fFriction.slip;
                wheelHit.sidewaysSlip = sFriction.slip;
            }

            prevFreeRollingAngularVelocity = wheel.freeRollingAngularVelocity;
        }

        /// <summary>
        /// Updates force values, calculates force vector and applies it to the rigidbody.
        /// </summary>
        private void UpdateForces()
        {
            if (hasHit)
            {
                // Use alternate normal when encountering obstracles that have sharp edges in which case raycastHit.normal will alwyas point up.
                // Alternate normal cannot be used when on flat surface because of inaccuracies which cause vehicle to creep forward or in reverse.
                // Sharp edge detection is done via dot product of bot normals, if it differs it means that raycasHit.normal in not correct.

                // Cache most used values
                Vector3 wheelHitPoint = wheelHit.point;
                Vector3 raycastHitNormal = wheelHit.raycastHit.normal;

                // Hit direction
                hitDir.x = wheel.worldPosition.x - wheelHitPoint.x;
                hitDir.y = wheel.worldPosition.y - wheelHitPoint.y;
                hitDir.z = wheel.worldPosition.z - wheelHitPoint.z;

                // Alternate normal
                float distance = Mathf.Sqrt(hitDir.x * hitDir.x + hitDir.y * hitDir.y + hitDir.z * hitDir.z);
                alternateForwardNormal.x = hitDir.x / distance;
                alternateForwardNormal.y = hitDir.y / distance;
                alternateForwardNormal.z = hitDir.z / distance;

                if (Vector3.Dot(raycastHitNormal, transformUp) > 0.1f)
                {
                    // Spring force
                    float suspensionForceMagnitude = Mathf.Clamp(spring.force + damper.force, 0.0f, Mathf.Infinity);

                    // Obstracle force
                    float obstracleForceMagnitude = 0f;

                    // Abs speed
                    float absSpeed = fFriction.speed;
                    if (absSpeed < 0) absSpeed = -absSpeed;

                    if (absSpeed < 8f)
                    {
                        // Dot between normal and alternate normal
                        projectedNormal = Vector3.ProjectOnPlane(wheelHit.normal, wheel.right);
                        float distace = Mathf.Sqrt(projectedNormal.x * projectedNormal.x + projectedNormal.y * projectedNormal.y + projectedNormal.z * projectedNormal.z);
                        projectedNormal.x /= distace;
                        projectedNormal.y /= distace;
                        projectedNormal.z /= distace;

                        projectedAltNormal = Vector3.ProjectOnPlane(alternateForwardNormal, wheel.right);
                        distace = Mathf.Sqrt(projectedAltNormal.x * projectedAltNormal.x + projectedAltNormal.y * projectedAltNormal.y + projectedAltNormal.z * projectedAltNormal.z);
                        projectedAltNormal.x /= distace;
                        projectedAltNormal.y /= distace;
                        projectedAltNormal.z /= distace;

                        float dot = Vector3.Dot(projectedNormal, projectedAltNormal);

                        // Abs dot
                        if (dot < 0) dot = -dot;

                        obstracleForceMagnitude = (1f - dot) * suspensionForceMagnitude * -Mathf.Sign(wheelHit.angleForward);
                    }

                    totalForce.x = obstracleForceMagnitude * wheel.forward.x
                        + suspensionForceMagnitude * raycastHitNormal.x
                        + wheelHit.sidewaysDir.x * -sFriction.force
                        + wheelHit.forwardDir.x * fFriction.force;

                    totalForce.y = obstracleForceMagnitude * wheel.forward.y
                        + suspensionForceMagnitude * raycastHitNormal.y
                        + wheelHit.sidewaysDir.y * -sFriction.force
                        + wheelHit.forwardDir.y * fFriction.force;

                    totalForce.z = obstracleForceMagnitude * wheel.forward.z
                        + suspensionForceMagnitude * raycastHitNormal.z
                        + wheelHit.sidewaysDir.z * -sFriction.force
                        + wheelHit.forwardDir.z * fFriction.force;

                    forcePoint.x = (wheelHitPoint.x * 3 + spring.targetPoint.x) / 4f;
                    forcePoint.y = (wheelHitPoint.y * 3 + spring.targetPoint.y) / 4f;
                    forcePoint.z = (wheelHitPoint.z * 3 + spring.targetPoint.z) / 4f;

                    parentRigidbody.AddForceAtPosition(totalForce, forcePoint);

                    if (applyForceToOthers)
                    {
                        if (wheelHit.raycastHit.rigidbody)
                        {
                            wheelHit.raycastHit.rigidbody.AddForceAtPosition(-totalForce, forcePoint);
                        }
                    }
                }
            }
        }


        #region Classes
        /*****************************/
        /* CLASSES                   */
        /*****************************/

        /// <summary>
        /// All info related to longitudinal force calculation.
        /// </summary>
        [System.Serializable]
        public class Friction
        {
            public float forceCoefficient = 1.1f;
            public float slipCoefficient = 1;
            public float maxForce;
            public float slip;
            public float speed;
            public float force;
        }


        /// <summary>
        /// Suspension part.
        /// </summary>
        [System.Serializable]
        public class Damper
        {
            public AnimationCurve dampingCurve = null;
            public float unitBumpForce = 800.0f;
            public float unitReboundForce = 1000.0f;
            public float force;
            public float maxForce;
        }


        /// <summary>
        /// Suspension part.
        /// </summary>
        [System.Serializable]
        public class Spring
        {
            public float maxLength = 0.3f;
            public AnimationCurve forceCurve = null;
            public float maxForce = 22000.0f;

            public float length;
            public float prevLength;
            public float compressionPercent;
            public float force;
            public float velocity;
            public Vector3 targetPoint;

            public float overflow;
            public float prevOverflow;
            public float overflowVelocity;
            public float bottomOutForce;

            public bool bottomedOut;
            public bool overExtended;
        }


        /// <summary>
        /// Contains everything wheel related, including rim and tire.
        /// </summary>
        [System.Serializable]
        public class Wheel
        {
            public float mass = 25.0f;
            public float rimOffset = 0f;
            public float tireRadius = 0.4f;
            public float width = 0.25f;

            public float rpm;

            public Vector3 prevWorldPosition;
            public Vector3 worldPosition;
            public Vector3 prevGroundPoint;
            public Quaternion worldRotation;

            public AnimationCurve camberCurve = null;
            public float camberAngle;

            public float inertia;

            public float angularVelocity;
            public float freeRollingAngularVelocity;
            public float residualAngularVelocity;
            public float steerAngle;
            public float rotationAngle;
            public GameObject visual;
            public GameObject nonRotating;
            public GameObject rim;
            public Transform rimCollider;

            public Vector3 up;
            public Vector3 inside;
            public Vector3 forward;
            public Vector3 right;
            public Vector3 velocity;
            public Vector3 prevVelocity;
            public Vector3 acceleration;

            public float tireLoad;

            public float motorTorque;
            public float brakeTorque;

            public Vector3 nonRotatingPostionOffset;

            /// <summary>
            /// Calculation of static parameters and creation of rim collider.
            /// </summary>
            public void Initialize(WheelController wc)
            {
                // Precalculate wheel variables
                inertia = 0.5f * mass * (tireRadius * tireRadius + tireRadius * tireRadius);

                // Instantiate rim (prevent ground passing through the side of the wheel)
                rim = new GameObject();
                rim.name = "RimCollider";
                rim.transform.position = wc.transform.position + wc.transform.right * rimOffset * (int)wc.vehicleSide;
                rim.transform.parent = wc.transform;
                rim.layer = LayerMask.NameToLayer("Ignore Raycast");

                if (wc.useRimCollider && visual != null)
                {
                    MeshFilter mf = rim.AddComponent<MeshFilter>();
                    mf.name = "Rim Mesh Filter";
                    mf.mesh = wc.GenerateRimColliderMesh(visual.transform);
                    mf.mesh.name = "Rim Mesh";

                    MeshCollider mc = rim.AddComponent<MeshCollider>();
                    mc.name = "Rim MeshCollider";
                    mc.convex = true;

                    PhysicMaterial material = new PhysicMaterial();
                    material.staticFriction = 0f;
                    material.dynamicFriction = 0f;
                    material.bounciness = 0.3f;
                    mc.material = material;

                    wc.wheel.rimCollider = rim.transform;
                }
            }

            public void GenerateCamberCurve(float camberAtBottom, float camberAtTop)
            {
                AnimationCurve ac = new AnimationCurve();
                ac.AddKey(0.0f, camberAtBottom);
                ac.AddKey(1.0f, camberAtTop);
                camberCurve = ac;
            }
        }


        /// <summary>
        /// Contains RaycastHit and extended hit data.
        /// </summary>
        [System.Serializable]
        public class WheelHit
        {
            [SerializeField]
            public RaycastHit raycastHit;
            public float angleForward;
            public float distanceFromTire;
            public Vector2 offset;

            [HideInInspector]
            public float weight;
            public bool valid = false;
            public float curvatureOffset;
            public Vector3 groundPoint;

            public WheelHit() { }

            public void Copy(WheelHit hit, bool copyHit)
            {
                if (copyHit) raycastHit = hit.raycastHit;
                angleForward = hit.angleForward;
                distanceFromTire = hit.distanceFromTire;
                offset.x = hit.offset.x;
                offset.y = hit.offset.y;
                weight = hit.weight;
                curvatureOffset = hit.curvatureOffset;
            }

            /// <summary>
            /// The point of contact between the wheel and the ground.
            /// </summary>
            public Vector3 point
            {
                get
                {
                    return groundPoint;
                }
            }

            /// <summary>
            /// The normal at the point of contact 
            /// </summary>
            public Vector3 normal
            {
                get
                {
                    return raycastHit.normal;
                }
            }

            /// <summary>
            /// The direction the wheel is pointing in.
            /// </summary>
            public Vector3 forwardDir;

            /// <summary>
            /// Tire slip in the rolling direction.
            /// </summary>
            public float forwardSlip;

            /// <summary>
            /// The sideways direction of the wheel.
            /// </summary>
            public Vector3 sidewaysDir;

            /// <summary>
            /// The slip in the sideways direction.
            /// </summary>
            public float sidewaysSlip;

            /// <summary>
            /// The magnitude of the force being applied for the contact. [N]
            /// </summary>
            public float force;

            // WheelCollider compatibility variables
            public Collider collider
            {
                get
                {
                    return raycastHit.collider;
                }
            }
        }
        #endregion


        #region Functions
        /*****************************/
        /* FUNCTIONS                 */
        /*****************************/

        public void Initialize()
        {
            // Objects
            if (parent == null) parent = FindParent();
            if (wheel == null) wheel = new Wheel();
            if (spring == null) spring = new Spring();
            if (damper == null) damper = new Damper();
            if (fFriction == null) fFriction = new Friction();
            if (sFriction == null) sFriction = new Friction();

            // Curves
            if (springCurve == null || springCurve.keys.Length == 0) springCurve = GenerateDefaultSpringCurve();
            if (damperCurve == null || damperCurve.keys.Length == 0) damperCurve = GenerateDefaultDamperCurve();
            if (wheel.camberCurve == null || wheel.camberCurve.keys.Length == 0) wheel.GenerateCamberCurve(0, 0);
            if (activeFrictionPreset == null) activeFrictionPreset = FrictionPreset.TarmacDry;

            //Other
            if (vehicleSide == Side.Auto && parent != null) vehicleSide = DetermineSide(transform.position, parent.transform);
        }


        private GameObject FindParent()
        {
            Transform t = transform;
            while (t != null)
            {
                if (t.GetComponent<Rigidbody>())
                {
                    return t.gameObject;
                }
                else
                {
                    t = t.parent;
                }
            }
            return null;
        }

        private AnimationCurve GenerateDefaultSpringCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0.0f, 0.0f);
            ac.AddKey(1.0f, 1.0f);
            return ac;
        }


        private AnimationCurve GenerateDefaultDamperCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0f, 0f);
            ac.AddKey(100f, 400f);
            return ac;
        }


        public Mesh GenerateRimColliderMesh(Transform rt)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            var halfWidth = wheel.width / 1.6f;
            float theta = 0.0f;
            float startAngleOffset = Mathf.PI / 18.0f;
            float x = tireRadius * 0.5f * Mathf.Cos(theta);
            float y = tireRadius * 0.5f * Mathf.Sin(theta);
            Vector3 pos = rt.InverseTransformPoint(wheel.worldPosition + wheel.up * y + wheel.forward * x);
            Vector3 newPos = pos;

            int vertexIndex = 0;
            for (theta = startAngleOffset; theta <= Mathf.PI * 2 + startAngleOffset; theta += Mathf.PI / 12.0f)
            {
                if (theta <= Mathf.PI - startAngleOffset)
                {
                    x = tireRadius * 0.93f * Mathf.Cos(theta);
                    y = tireRadius * 0.93f * Mathf.Sin(theta);
                }
                else
                {
                    x = tireRadius * 0.1f * Mathf.Cos(theta);
                    y = tireRadius * 0.1f * Mathf.Sin(theta);
                }

                newPos = rt.InverseTransformPoint(wheel.worldPosition + wheel.up * y + wheel.forward * x);

                // Left Side
                Vector3 p0 = pos - rt.InverseTransformDirection(wheel.right) * halfWidth;
                Vector3 p1 = newPos - rt.InverseTransformDirection(wheel.right) * halfWidth;

                // Right side
                Vector3 p2 = pos + rt.InverseTransformDirection(wheel.right) * halfWidth;
                Vector3 p3 = newPos + rt.InverseTransformDirection(wheel.right) * halfWidth;

                vertices.Add(p0);
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);

                // Triangles (double sided)
                // 013
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 0);

                // 023
                triangles.Add(vertexIndex + 0);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);

                pos = newPos;
                vertexIndex += 4;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }


        /// <summary>
        /// Average of multiple Vector3's
        /// </summary>
        private Vector3 Vector3Average(List<Vector3> vectors)
        {
            Vector3 sum = Vector3.zero;
            foreach (Vector3 v in vectors)
            {
                sum += v;
            }
            return sum / vectors.Count;
        }


        /// <summary>
        /// Calculates an angle between two vectors in relation a normal.
        /// </summary>
        /// <param name="v1">First Vector.</param>
        /// <param name="v2">Second Vector.</param>
        /// <param name="n">Angle around this vector.</param>
        /// <returns>Angle in degrees.</returns>
        private float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            return Mathf.Atan2(
                Vector3.Dot(n, Vector3.Cross(v1, v2)),
                Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Determines on what side of the vehicle a point is. 
        /// </summary>
        /// <param name="pointPosition">Position of the point in question.</param>
        /// <param name="referenceTransform">Position of the reference transform.</param>
        /// <returns>Enum Side [Left,Right] (int)[-1,1]</returns>
        public Side DetermineSide(Vector3 pointPosition, Transform referenceTransform)
        {
            Vector3 relativePoint = referenceTransform.InverseTransformPoint(pointPosition);

            if (relativePoint.x < 0.0f)
            {
                return WheelController.Side.Left;
            }
            else
            {
                return WheelController.Side.Right;
            }
        }


        #endregion

    }
}
