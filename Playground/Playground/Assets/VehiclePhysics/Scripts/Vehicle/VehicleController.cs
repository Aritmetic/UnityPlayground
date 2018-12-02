using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NWH.WheelController3D;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Main class controlling all the other parts of the vehicle.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CenterOfMass))]
    public class VehicleController : MonoBehaviour
    {
        /// <summary>
        /// If disabled vehicle will be suspended with only basic functions working. It will still interact with environment but will not be driveable.
        /// It is recommended to set this to false if vehicle is inactive as it helps performance.
        /// </summary>
        [Tooltip("If disabled vehicle will be suspended with only basic functions working. It will still interact with environment but will not be driveable." +
            " It is recommended to set this to false if vehicle is inactive as it helps performance.")]
        [SerializeField]
        private bool active = true;

        [HideInInspector]
        public InputStates input = new InputStates();

        [SerializeField]
        public Sound sound = new Sound();

        [SerializeField]
        public Effects effects = new Effects();

        [SerializeField]
        public Steering steering = new Steering();

        [SerializeField]
        public Engine engine = new Engine();

        [SerializeField]
        public Transmission transmission = new Transmission();

        [SerializeField]
        public List<Axle> axles = new List<Axle>();

        [SerializeField]
        public Brakes brakes = new Brakes();

        private List<Wheel> wheels = new List<Wheel>();

        [SerializeField]
        public Tracks tracks = new Tracks();

        [HideInInspector]
        [SerializeField]
        public GroundDetection groundDetection;

        [SerializeField]
        public DrivingAssists drivingAssists = new DrivingAssists();

        [SerializeField]
        public DamageHandler damage = new DamageHandler();

        [SerializeField]
        public Fuel fuel = new Fuel();

        [SerializeField]
        public Rigging rigging = new Rigging();

        [SerializeField]
        public FlipOver flipOver = new FlipOver();

        [HideInInspector]
        [SerializeField]
        public TrailerHandler trailer = new TrailerHandler();

        [HideInInspector]
        public Metrics metrics = new Metrics();

        [HideInInspector]
        public float forwardSlipThreshold = 0.35f;

        [HideInInspector]
        public float sideSlipThreshold = 0.1f;

        [HideInInspector]
        public float speedLimiter = 0f;

        [HideInInspector]
        public bool freezeWhenStill = true;

        [HideInInspector]
        public bool freezeWhenInactive = true;

        private bool frozen;
        private bool wasFrozen;
        private float forwardVelocity;
        private float forwardAcceleration;
        private float load;
        private Vector3 velocity;
        private Vector3 prevVelocity;
        private Vector3 acceleration;
        private bool wheelSpin;
        private bool wheelSkid;

        [HideInInspector]
        public Rigidbody vehicleRigidbody;

        public enum VehicleCollisionState { Enter, Stay, None }
        private VehicleCollisionState collisionState = VehicleCollisionState.None;
        private Collision collisionInfo;

        /// <summary>
        /// If true vehicle can be driven. If false vehicle will be in a suspended state with only the minimal functions working and will not respond to input.
        /// </summary>
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                if (active == false && value == true)
                    Activate();
                else if (active == true && value == false)
                    Suspend();

                active = value;
            }
        }

        /// <summary>
        /// Velocity in forward direction in local coordinates (z-forward).
        /// </summary>
        public float ForwardVelocity
        {
            get
            {
                return forwardVelocity;
            }
        }

        /// <summary>
        /// Speed in forward direction in local coordinates (z-forward). Always positive.
        /// For positive/negative version use ForwardVelocity.
        /// </summary>
        public float Speed
        {
            get
            {
                return Mathf.Abs(ForwardVelocity);
            }
        }

        /// <summary>
        /// Speed in kilometers per hour.
        /// </summary>
        [ShowInTelemetry]
        public float SpeedKPH
        {
            get
            {
                return Speed * 3.6f;
            }
        }

        /// <summary>
        /// Speed is (US) miles per hour.
        /// </summary>
        [ShowInTelemetry]
        public float SpeedMPH
        {
            get
            {
                return Speed * 2.237f;
            }
        }

        /// <summary>
        /// Amount of load vehicle / engine is under. 0 to 1. 
        /// </summary>
        public float Load
        {
            get
            {
                return load;
            }
        }

        /// <summary>
        /// Speed at the wheels. Only powered wheels are taken into account.
        /// If no powered axles ForwardVelocity of rigidbody will be returned instead.
        /// </summary>
        public float WheelSpeed
        {
            get
            {
                float rpmSum = 0;
                int poweredAxleCount = 0;
                float radiusSum = 0;
                foreach (Axle axle in axles) if (axle.IsPowered)
                    {
                        poweredAxleCount++;
                        rpmSum += Mathf.Abs(axle.RPM);
                        radiusSum += axle.leftWheel.Radius;
                        radiusSum += axle.rightWheel.Radius;
                    }

                if (poweredAxleCount != 0)
                {
                    float avgRadius = radiusSum / (poweredAxleCount * 2);
                    float avgRpm = rpmSum / poweredAxleCount;
                    return avgRadius * avgRpm * 0.10472f;
                }
                else
                {
                    return Mathf.Abs(ForwardVelocity);
                }
            }
        }

        /// <summary>
        /// Acceleration in forward direction in local coordinates (z-forward).
        /// </summary>
        public float ForwardAcceleration
        {
            get
            {
                return forwardAcceleration;
            }
        }

        /// <summary>
        /// Acceleration in local coordinates (z-forward)
        /// </summary>
        public Vector3 Acceleration
        {
            get
            {
                return acceleration;
            }
        }

        /// <summary>
        /// Direction the vehicle is currently traveling in. 1 for forward, -1 for reverse and 0 for being perfectly still.
        /// </summary>
        public float Direction
        {
            get
            {
                float velZ = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity).z;

                if (velZ > 0)
                    return 1;
                else if (velZ < 0)
                    return -1;
                else
                    return 0;
            }
        }

        /// <summary>
        /// List of all wheels attached to this vehicle.
        /// </summary>
        public List<Wheel> Wheels
        {
            get
            {
                return wheels;
            }
        }

        /// <summary>
        /// True if any of the wheels is spinning out (slipping in the forward direction).
        /// </summary>
        [ShowInTelemetry]
        public bool WheelSpin
        {
            get { return wheelSpin; }
        }

        /// <summary>
        /// True if any of the wheels is skidding (slipping in the lateral / side direction)
        /// </summary>
        [ShowInTelemetry]
        public bool WheelSkid
        {
            get { return wheelSkid; }
        }

        /// <summary>
        /// Returns the state of the current collision.
        /// </summary>
        public VehicleCollisionState CollisionState
        {
            get
            {
                return collisionState;
            }
        }

        /// <summary>
        /// Returns the info on the current collision.
        /// </summary>
        public Collision CollisionInfo
        {
            get
            {
                return collisionInfo;
            }
        }


        private void Start()
        {
            #region AdjustDeltaTime
            // Delete this region if you want to use fixed delta time > 0.017. Using tight suspension setups with higher fixed delta time values is not recommended.
            if(Time.fixedDeltaTime > 0.017f)
            {
                Debug.LogWarning("Using fixed delta time setting higher than 0.017 is not recommended. Changing fixed delta time to 0.017." +
                    " This setting can be found under Project Settings > Time.");
                Time.fixedDeltaTime = 0.017f;
            }
            #endregion

            Initialize();
        }


        private void Initialize()
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
            vehicleRigidbody.maxAngularVelocity = 10f;

            // Initialize components
            if (groundDetection == null)
            {
                groundDetection = FindGroundDetectionComponent();
            }

            steering.Initialize(this);
            foreach (Axle axle in axles) axle.Initialize(this);

            wheels = GetAllWheels();

            engine.Initialize(this);
            transmission.Initialize(this);
            sound.Initialize(this);
            effects.Initialize(this);
            damage.Initialize(this);
            fuel.Initialize(this);

            if (trailer.isTrailer)
            {
                if (!trailer.attached)
                {
                    Suspend();
                }
            }

            trailer.Initialize(this);

            if (input == null)
                input = new InputStates();
            input.Initialize(this);

            drivingAssists.Initialize(this);
            flipOver.Initialize(this);
            metrics.Initialize(this);

            // Suspend if not active and opposite
            if (!active)
                Suspend();
            else if (active)
                Activate();

            // Tracked
            if (tracks.trackedVehicle)
            {
                tracks.Initialize(this);
                if(tracks.wheelEnlargementCoefficient > 1f)
                {
                    float multiplierSum = 0;
                    foreach (Wheel wheel in wheels)
                    {
                        float originalRadius = wheel.wheelController.radius;
                        float newRadius = originalRadius * tracks.wheelEnlargementCoefficient;
                        multiplierSum += newRadius / originalRadius;
                        wheel.wheelController.radius = newRadius;
                        wheel.wheelController.transform.position += wheel.wheelController.transform.up * (newRadius - originalRadius);
                        wheel.wheelController.trackedOffset = newRadius - originalRadius;
                        wheel.wheelController.SetCamber(0);
                    }
                    transmission.gearMultiplier *= (multiplierSum / wheels.Count);
                    brakes.maxTorque *= transmission.gearMultiplier;
                    forwardSlipThreshold = Mathf.Infinity;
                    sideSlipThreshold = Mathf.Infinity;
                }
            }
        }


        private void FixedUpdate()
        {
            prevVelocity = velocity;
            velocity = transform.InverseTransformDirection(vehicleRigidbody.velocity);
            acceleration = (velocity - prevVelocity) / Time.fixedDeltaTime;

            forwardVelocity = velocity.z;
            forwardAcceleration = acceleration.z;

            if (engine.starting || engine.stopping)
                engine.Update();

            foreach(Wheel wheel in wheels)
            {
                wheel.Update();
            }

            brakes.Update(this);

            if (active)
            {
                load = GetLoad();

                // Detect slip and skid
                DetectWheelSkid();
                DetectWheelSpin();

                // Update axles
                foreach (Axle axle in axles) axle.Update();

                // Change friction based on surface
                if (groundDetection != null)
                {
                    foreach (Wheel wheel in Wheels)
                    {
                        GroundDetection.GroundEntity groundEntity = groundDetection.GetCurrentGroundEntity(wheel.WheelController);

                        if (groundEntity != null)
                        {
                            wheel.WheelController.SetActiveFrictionPreset(groundEntity.frictionPresetEnum);
                        }
                    }
                }

                if (!trailer.isTrailer)
                {
                    engine.Update();

                    transmission.Update();

                    // Split torque between wheels
                    transmission.TorqueSplit(
                        transmission.TransmitTorque(engine.Torque),
                        transmission.TransmitRPM(engine.RPM)
                    );
                }

                trailer.Update();

                // Update systems
                drivingAssists.Update();

                // Steer
                steering.Steer();
                steering.AdjustGeometry();

                metrics.Update();
                fuel.Update();
            }

            // Tracks
            if (tracks.trackedVehicle)
            {
                tracks.Update();
            }

            // Speed limiter
            if (speedLimiter != 0)
            {
                float regulatorThreshold = 0.8f;
                if (Speed > speedLimiter * regulatorThreshold)
                {
                    float powerReduction = Mathf.Clamp01((Speed - (speedLimiter * regulatorThreshold)) / (speedLimiter * (1f - regulatorThreshold)));
                    engine.TcsPowerReduction = powerReduction * powerReduction;
                }
            }

            // Flip over
            flipOver.Update();

            // Freeze when still
            // Determine if needs freezing to prevent slip when stationary on sloped surfaces
            if(freezeWhenStill && !flipOver.flippedOver)
            {
                if( (active && Speed < 0.1f && Mathf.Abs(input.Vertical) < 0.1f) || (!active && Speed < 0.2f && freezeWhenInactive))
                {
                    frozen = true;
                }
                else
                {
                    frozen = false;
                }
            }

            // Toggle state between frozen and not frozen
            if(frozen && !wasFrozen)
            {
                vehicleRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY;
            }
            else if(!frozen && wasFrozen)
            {
                vehicleRigidbody.constraints = RigidbodyConstraints.None;
            }
            wasFrozen = frozen;
        }

        public void Update()
        {
            rigging.Update(this);
        }

        public void LateUpdate()
        {
            effects.Update();
            sound.Update();
            if (damage.enabled)
                damage.Update();
        }

        /// <summary>
        /// Get wheels from specific range of axles.
        /// </summary>
		private List<Wheel> GetWheels(int from, int to)
        {
            List<Wheel> whs = new List<Wheel>();
            for (int i = from; i <= to; i++)
            {
                whs.Add(axles[i].leftWheel);
                whs.Add(axles[i].rightWheel);
            }
            return whs;
        }

        /// <summary>
        /// Gets all wheels from all axles.
        /// </summary>
        private List<Wheel> GetAllWheels()
        {
            List<Wheel> whs = new List<Wheel>();
            foreach (Axle axle in axles)
            {
                whs.Add(axle.leftWheel);
                whs.Add(axle.rightWheel);
            }
            return whs;
        }

        private float GetLoad()
        {
            float rpmCoeff = engine.RPMPercent;
            float powerCoeff = engine.Power / engine.maxPower;

            float load = Mathf.Clamp01(powerCoeff * 0.6f + rpmCoeff * 0.4f);
            return load;
        }

        /// <summary>
        /// Average RPM of all wheels on the vehicle.
        /// </summary>
        public float GetAverageWheelRPM()
        {
            float allWheelRpmSum = 0;
            int wheelCount = 0;
            foreach (Wheel wheel in Wheels)
            {
                allWheelRpmSum += wheel.SmoothRPM;
                wheelCount++;
            }
            float averageRpm = 0;
            if (wheelCount > 0)
            {
                averageRpm = allWheelRpmSum / wheelCount;
            }
            return averageRpm;
        }

        /// <summary>
        /// Average unsmoothed RPM of all wheels on the vehicle.
        /// </summary>
        /// <returns></returns>
        public float GetRawAverageWheelRPM()
        {
            float allWheelRpmSum = 0;
            int wheelCount = 0;
            foreach (Wheel wheel in Wheels)
            {
                allWheelRpmSum += wheel.RPM;
                wheelCount++;
            }
            float averageRpm = 0;
            if (wheelCount > 0)
            {
                averageRpm = allWheelRpmSum / wheelCount;
            }
            return averageRpm;
        }


        public float GetCorrectWheelRpm(Wheel wheel)
        {
            float wheelCircumfence = 2 * Mathf.PI * wheel.Radius;
            return (Speed / wheelCircumfence) * 60f;
        }


        /// <summary>
        /// True if any of the wheels is spinning.
        /// </summary>
        /// <returns></returns>
        public bool DetectWheelSpin()
        {
            foreach (Axle a in axles)
            {
                if (a.IsPowered && a.WheelSpin)
                {
                    return wheelSpin = true;
                }
            }
            return wheelSpin = false;
        }


        /// <summary>
        /// True if any of the wheels is drifting / skidding.
        /// </summary>
        public bool DetectWheelSkid()
        {
            foreach (Wheel wheel in Wheels)
            {
                if (wheel.HasSideSlip)
                {
                    return wheelSkid = true;
                }
            }
            return wheelSkid = false;
        }


        private void Suspend()
        {
            active = false;
            if(engine.stopOnDisable && engine.IsRunning) engine.Stop();
            effects.lights.enabled = false;
            foreach (Wheel wheel in Wheels)
            {
                wheel.SetBrakeIntensity(0.2f);
                wheel.Suspend();
            }
            sound.Disable();
        }


        private void Activate()
        {
            active = true;
            if(engine.runOnEnable && !engine.IsRunning) engine.Start();
            effects.lights.enabled = true;
            foreach (Wheel wheel in Wheels)
            {
                wheel.ResetBrakes(0);
                wheel.Activate();
            }
            sound.Enable();
        }


        public VehicleCollisionState GetCollisionState()
        {
            return collisionState;
        }


        private void OnCollisionEnter(Collision collision)
        {
            collisionState = VehicleCollisionState.Enter;
            collisionInfo = collision;
        }


        private void OnCollisionStay(Collision collision)
        {
            collisionState = VehicleCollisionState.Stay;
            collisionInfo = collision;

            float accelerationMagnitude = Acceleration.magnitude;
            int collisionHash = collision.GetHashCode();

            if (damage.previousCollisionHash != collisionHash && accelerationMagnitude > damage.decelerationThreshold)
            {
                sound.crashComponent.Play(this);

                if (damage.enabled)
                {
                    damage.Damage += accelerationMagnitude;
                    damage.Enqueue(collision, accelerationMagnitude);
                }
            }
            damage.previousCollisionHash = collisionHash;
        }


        private void OnCollisionExit(Collision collision)
        {
            collisionState = VehicleCollisionState.None;
            collisionInfo = null;
        }


        public void Reset()
        {
            SetDefaults();
        }

        /// <summary>
        /// Resets the vehicle to default state.
        /// Will find WheelControllers, assign axles, set default values for all 
        /// fields and assign default audio clips from resources folder.
        /// </summary>
        public void SetDefaults()
        {
            groundDetection = FindGroundDetectionComponent();

            // Axles
            if (axles != null && axles.Count == 0)
            {
                axles = GetAxles();

                if(axles == null || axles.Count == 0)
                {
                    Debug.LogWarning("WheelControllers do not exist or not properly set up. " +
                        "Skipping axle initialization - axles will have to be assigned manually.");
                    return;
                }

                if (axles != null && axles.Count != 0)
                {
                    for (int i = 0; i < axles.Count; i++)
                    {
                        Axle.Geometry g = new Axle.Geometry();
                        if (i == 0)
                        {
                            g.steerCoefficient = 1f;
                            g.antiRollBarForce = axles[i].leftWheel.WheelController.springMaximumForce / 2f;
                            axles[i].geometry = g;
                        }
                        else if (i == axles.Count - 1)
                        {
                            g.antiRollBarForce = axles[i].leftWheel.WheelController.springMaximumForce / 2.5f;
                            axles[i].geometry = g;
                            axles[i].handbrakeCoefficient = 1f;
                        }

                        axles[i].powerCoefficient = 1f;
                    }
                }

                foreach(Axle axle in axles)
                {
                    axle.Initialize(this);
                }
            }

            // Set sound defaults
            if (sound == null) sound = new Sound();
            sound.SetDefaults();

            // Adjust center of mass
            try
            {
                ResetCOM();
            }
            catch
            {
                Debug.LogWarning("Center of mass could not be setup automatically. You might want to do this manually as COM drastically affects vehicle behavior.");
            }

            if(Application.isPlaying)
            {
                Initialize();
            }
        }


        private GroundDetection FindGroundDetectionComponent()
        {
            var groundDetectionObjects = FindObjectsOfType(typeof(GroundDetection));
            if (groundDetectionObjects.Length > 0)
            {
                if (groundDetectionObjects.Length > 1)
                    Debug.LogWarning("More than one GroundDetection component was found in the scene, only one is needed and so onlyt the first one will be used.");

                return (GroundDetection)groundDetectionObjects[0];
            }
            else
            {
                Debug.LogWarning("No GroundDetection component found in the scene. GroundDetection needs to be set up for surface effects such as skidmarks to work.");

                return null;
            }
        }

        /// <summary>
        /// Finds and resets center of mass of the vehicle in relation to the position of all WheelController components.
        /// </summary>
        public void ResetCOM()
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
            CenterOfMass com = GetComponent<CenterOfMass>();
            Vector3 centerPoint = Vector3.zero;
            Vector3 pointSum = Vector3.zero;
            int count = 0;
            foreach (Wheel wheel in GetAllWheels())
            {
                pointSum += transform.InverseTransformPoint(wheel.WheelController.transform.position);
                count++;
            }
            if (count == 0) return;

            centerPoint = pointSum / count;
            centerPoint -= GetAllWheels()[0].WheelController.springLength * 0.45f * transform.up;
            vehicleRigidbody.ResetCenterOfMass();
            com.centerOfMassOffset = centerPoint - vehicleRigidbody.centerOfMass;
        }


        /// <summary>
        /// Gets all the axles the vehicle has. Will search all child objects for WheelController components and
        /// return a list of axles, each with 2 Wheel Controllers paired by their Z position relative to the parent.
        /// Axles will be returned in order from front to back.
        /// </summary>
        public List<Axle> GetAxles()
        {
            List<Axle> axles = new List<Axle>();
            List<WheelController> wcs = transform.GetComponentsInChildren<WheelController>().ToList();

            if (wcs.Count > 0)
            {
                List<WheelController> leftWheels = new List<WheelController>();
                List<WheelController> rightWheels = new List<WheelController>();

                foreach (WheelController wc in wcs)
                {
                    if (wc.VehicleSide == WheelController.Side.Left)
                        leftWheels.Add(wc);
                    else if (wc.VehicleSide == WheelController.Side.Right)
                        rightWheels.Add(wc);
                }

                if (leftWheels.Count != rightWheels.Count)
                {
                    Debug.LogWarning("There is unequal number of Left and Right wheels (L:" + leftWheels.Count + ", R:" + rightWheels.Count + "). " +
                        "You will have to assign WheelControllers to axles manually in the inspector of VehicleController.");
                    return null;
                }


                if ((leftWheels.Count + rightWheels.Count) % 2 != 0)
                {
                    Debug.LogWarning("There is odd number of wheels (trike?)." +
                        "You will have to assign WheelControllers to axles manually in the inspector of VehicleController.");
                    return null;
                }


                if (leftWheels.Count == 0 && rightWheels.Count == 0 && wcs.Count != 0)
                {
                    Debug.LogWarning("No Left or Right wheels have been found but multiple Center wheels found. Check if vehicle side is set properly inside " +
                        "WheelController inspectors.");
                    return null;
                }

                leftWheels = leftWheels.OrderByDescending(x => GetWheelZPosition(x, this)).ToList();
                rightWheels = rightWheels.OrderByDescending(x => GetWheelZPosition(x, this)).ToList();

                for (int i = 0; i < leftWheels.Count; i++)
                {
                    Axle axle = new Axle();
                    axle.leftWheel = new Wheel(leftWheels[i], this);
                    axle.rightWheel = new Wheel(rightWheels[i], this);
                    axles.Add(axle);
                }
                return axles;
            }
            else
            {
                Debug.LogWarning("No WheelControllers found. You will have to add WheelControllers or setup axles manually.");
                return null;
            }
        }

        private float GetWheelZPosition(WheelController3D.WheelController wc, VehicleController vc)
        {
            return transform.InverseTransformPoint(wc.transform.position).z;
        }

        /// <summary>
        /// Calculates an angle between two vectors in relation a normal.
        /// </summary>
        /// <param name="v1">First Vector.</param>
        /// <param name="v2">Second Vector.</param>
        /// <param name="n">Angle around this vector.</param>
        /// <returns>Angle in degrees.</returns>
        public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            return Mathf.Atan2(
                Vector3.Dot(n, Vector3.Cross(v1, v2)),
                Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
        }

        public static float GetMouseHorizontal()
        {
            float percent = Mathf.Clamp(Input.mousePosition.x / Screen.width, -1f, 1f);
            if (percent < 0.5f)
                return -(0.5f - percent) * 2.0f;
            return (percent - 0.5f) * 2.0f;
        }

        public static float GetMouseVertical()
        {
            float percent = Mathf.Clamp(Input.mousePosition.y / Screen.height, -1f, 1f);
            if (percent < 0.5f)
                return -(0.5f - percent) * 2.0f;
            return (percent - 0.5f) * 2.0f;
        }
    }
}