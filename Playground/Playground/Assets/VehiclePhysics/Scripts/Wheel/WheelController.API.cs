using UnityEngine;
using System.Collections;
using System.Linq;

namespace NWH.WheelController3D
{
    /// <summary>
    /// API for WheelController
    /// </summary>
    public partial class WheelController : MonoBehaviour
    {

        #region UnityDefault

        /// <summary>
        /// Returns the position and rotation of the wheel.
        /// </summary>
        public void GetWorldPose(out Vector3 pos, out Quaternion quat)
        {
            pos = wheel.worldPosition;
            quat = wheel.worldRotation;
        }

        /// <summary>
        /// Brake torque on the wheel axle. [Nm]
        /// Must be positive (zero included).
        /// </summary>
        public float brakeTorque
        {
            get
            {
                return wheel.brakeTorque;
            }
            set
            {
                if (value >= 0)
                {
                    wheel.brakeTorque = value;
                }
                else
                {
                    wheel.brakeTorque = 0;
                    Debug.LogWarning("Brake torque must be positive and so was set to 0.");
                }
            }
        }

        /// <summary>
        /// Is the tractive surface touching the ground?
        /// Returns false if vehicle tipped over / tire sidewall is in contact.
        /// </summary>
        public bool isGrounded
        {
            get
            {
                if (hasHit)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Mass of the wheel. [kg]
        /// Typical values would be in range [20, 200]
        /// </summary>
        public float mass
        {
            get
            {
                return wheel.mass;
            }
            set
            {
                wheel.mass = value;
            }
        }

        /// <summary>
        /// Motor torque on the wheel axle. [Nm]
        /// Can be positive or negative based on direction.
        /// </summary>
        public float motorTorque
        {
            get
            {
                return wheel.motorTorque;
            }
            set
            {
                wheel.motorTorque = value;
            }
        }

        /// <summary>
        /// Equal to tireRadis but exists because of compatibility with inbuilt WheelCollider.
        /// Radius of the complete wheel. [meters]
        /// Must be larger than 0.
        /// </summary>
        public float radius
        {
            get
            {
                return tireRadius;
            }
            set
            {
                tireRadius = value;
                InitializeScanParams();
            }
        }

        /// <summary>
        /// Side offset of the rim. Positive value will result if wheel further from the vehicle. [meters]
        /// </summary>
        public float rimOffset
        {
            get
            {
                return wheel.rimOffset;
            }
            set
            {
                wheel.rimOffset = value;
            }
        }

        /// <summary>
        /// Radius (height) of the tire. [meters]
        /// </summary>
        public float tireRadius
        {
            get
            {
                return wheel.tireRadius;
            }
            set
            {
                wheel.tireRadius = value;
                InitializeScanParams();
            }
        }

        /// <summary>
        /// Width of the wheel. [meters]
        /// </summary>
        public float tireWidth
        {
            get
            {
                return wheel.width;
            }
            set
            {
                wheel.width = value;
                InitializeScanParams();
            }
        }

        /// <summary>
        /// Rotations per minute of the wheel around the axle. [rpm]
        /// </summary>
        public float rpm
        {
            get
            {
                return wheel.rpm;
            }
        }


        /// <summary>
        /// Steer angle around the wheel's up axis (with add-ons ignored). [deg]
        /// </summary>
        public float steerAngle
        {
            get
            {
                return wheel.steerAngle;
            }
            set
            {
                wheel.steerAngle = value;
            }
        }


        /// <summary>
        /// Returns Raycast info of the wheel's hit.
        /// Always check if the function returns true before using hit info
        /// as data will only be updated when wheel is hitting the ground (isGrounded == True).
        /// </summary>
        /// <param name="h">Standard Unity RaycastHit</param>
        /// <returns></returns>
        public bool GetGroundHit(out WheelHit hit)
        {
            hit = this.wheelHit;
            return hasHit;
        }

        #endregion

        #region Geometry

        /// <summary>
        /// Camber angle of the wheel. [deg]
        /// Negative angle means that the top of the wheel in closer to the vehicle than the bottom.
        /// </summary>
        public float camber
        {
            get
            {
                return wheel.camberAngle;
            }
        }

        /// <summary>
        /// Sets linear camber betwen the two values.
        /// </summary>
        /// <param name="camberAtTop"></param>
        /// <param name="camberAtBottom"></param>
        public void SetCamber(float camberAtTop, float camberAtBottom)
        {
            wheel.GenerateCamberCurve(camberAtTop, camberAtBottom);
        }

        /// <summary>
        /// Sets fixed camber.
        /// </summary>
        /// <param name="camber"></param>
        public void SetCamber(float camber)
        {
            wheel.GenerateCamberCurve(camber, camber);
        }

        /// <summary>
        /// Sets camber using AnimationCurve.
        /// </summary>
        /// <param name="curve"></param>
        public void SetCamber(AnimationCurve curve)
        {
            wheel.camberCurve = curve;
        }

        #endregion

        #region Spring

        /// <summary>
        /// Returns value in range [0,1] where 1 means spring is fully compressed.
        /// </summary>
        public float springCompression
        {
            get
            {
                return 1f - spring.compressionPercent;
            }
        }


        /// <summary>
        /// Spring velocity in relation to local vertical axis. [m/s]
        /// Positive on rebound (extension), negative on bump (compression).
        /// </summary>
        public float springVelocity
        {
            get
            {
                return spring.velocity;
            }
        }

        /// <summary>
        /// True when spring is fully compressed, i.e. there is no more spring travel.
        /// </summary>
        public bool springBottomedOut
        {
            get
            {
                return spring.bottomedOut;
            }
        }

        /// <summary>
        /// True when spring is fully extended.
        /// </summary>
        public bool springOverExtended
        {
            get
            {
                return spring.overExtended;
            }
        }

        /// <summary>
        /// Current spring force. [N]
        /// Can be written to for use in Anti-roll Bar script or similar.
        /// </summary>
        public float suspensionForce
        {
            get
            {
                return spring.force;
            }
            set
            {
                spring.force = value;
            }
        }

        /// <summary>
        /// Maximum spring force. [N]
        /// </summary>
        public float springMaximumForce
        {
            get
            {
                return spring.maxForce;
            }
            set
            {
                spring.maxForce = value;
            }
        }

        /// <summary>
        /// Spring force curve in relation to spring length.
        /// </summary>
        public AnimationCurve springCurve
        {
            get
            {
                return spring.forceCurve;
            }
            set
            {
                spring.forceCurve = value;
            }
        }

        /// <summary>
        /// Length of the spring when fully extended.
        /// </summary>
        public float springLength
        {
            get
            {
                return spring.maxLength;
            }
            set
            {
                spring.maxLength = value;
            }
        }

        /// <summary>
        /// Current length (travel) of spring.
        /// </summary>
        public float springTravel
        {
            get
            {
                return spring.length;
            }
        }

        /// <summary>
        /// Point in which spring and swingarm are in contact.
        /// </summary>
        public Vector3 springTravelPoint
        {
            get
            {
                return transform.position - transform.up * spring.length;
            }
        }

        #endregion

        #region Damper

        /// <summary>
        /// Current damper force.
        /// </summary>
        public float damperForce
        {
            get
            {
                return damper.force;
            }
        }

        /// <summary>
        /// Rebounding force at 1 m/s spring velocity
        /// </summary>
        public float damperUnitReboundForce
        {
            get
            {
                return damper.unitReboundForce;
            }
            set
            {
                damper.unitReboundForce = value;
            }
        }

        /// <summary>
        /// Bump force at 1 m/s spring velocity
        /// </summary>
        public float damperUnitBumpForce
        {
            get
            {
                return damper.unitBumpForce;
            }
            set
            {
                damper.unitBumpForce = value;
            }
        }

        /// <summary>
        /// Damper force curve in relation to spring velocity.
        /// </summary>
        public AnimationCurve damperCurve
        {
            get
            {
                return damper.dampingCurve;
            }
            set
            {
                damper.dampingCurve = value;
            }
        }

        #endregion

        #region Friction

        /// <summary>
        /// Returns _Friction object with longitudinal values.
        /// </summary>
        public Friction forwardFriction
        {
            get
            {
                return fFriction;
            }
            set
            {
                fFriction = value;
            }
        }

        /// <summary>
        /// Returns _Friction object with lateral values.
        /// </summary>
        public Friction sideFriction
        {
            get
            {
                return sFriction;
            }
            set
            {
                sFriction = value;
            }
        }


        public float MaxPutDownForce
        {
            get
            {
                return maxPutDownForce;
            }
        }


        public void SetActiveFrictionPreset(FrictionPreset fp)
        {
            activeFrictionPresetEnum = (FrictionPreset.FrictionPresetEnum)System.Enum.Parse(typeof(FrictionPreset.FrictionPresetEnum), fp.name);
            activeFrictionPreset = fp;
        }

        public void SetActiveFrictionPreset(FrictionPreset.FrictionPresetEnum fpe)
        {
            activeFrictionPresetEnum = fpe;
            activeFrictionPreset = GetFrictionPreset((int)fpe);
        }

        public FrictionPreset GetFrictionPreset(int index)
        {
            return activeFrictionPreset = FrictionPreset.FrictionPresetList[index];
        }

        #endregion

        #region Misc

        /// <summary>
        /// Returns Enum [Side] with the corresponding side of the vehicle a wheel is at [Left, Right]
        /// </summary>
        public Side VehicleSide
        {
            get
            {
                return vehicleSide;
            }
            set
            {
                vehicleSide = value;
            }
        }

        /// <summary>
        /// Returns vehicle speed in meters per second [m/s], multiply by 3.6 for [kph] or by 2.24 for [mph].
        /// </summary>
        public float speed
        {
            get
            {
                return fFriction.speed;
            }
        }

        /// <summary>
        /// Ground scan resolution in forward direction.
        /// </summary>
        public int ForwardScanResolution
        {
            get
            {
                return forwardScanResolution;
            }
            set
            {
                forwardScanResolution = value;

                if (forwardScanResolution < 1)
                {
                    forwardScanResolution = 1;
                    Debug.LogWarning("Forward scan resolution must be > 0.");
                }
                InitializeScanParams();
            }
        }

        /// <summary>
        /// Number of scan planes parallel to the wheel. 
        /// </summary>
        public int SideToSideScanResolution
        {
            get
            {
                return sideToSideScanResolution;
            }
            set
            {
                sideToSideScanResolution = value;
                if (sideToSideScanResolution < 1)
                {
                    sideToSideScanResolution = 1;
                    Debug.LogWarning("Side to side scan resolution must be > 0.");
                }
                InitializeScanParams();
            }
        }

        /// <summary>
        /// Returns wheel's parent object.
        /// </summary>
        public GameObject Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }

        /// <summary>
        /// Returns object that represents wheel's visual representation. Can be null in case the object is not assigned (not mandatory).
        /// </summary>
        public GameObject Visual
        {
            get
            {
                return wheel.visual;
            }
            set
            {
                wheel.visual = value;
            }
        }

        /// <summary>
        /// Object that follows the wheel position in everything but rotation around the axle.
        /// Can be used for brake calipers, external fenders, etc.
        /// </summary>
        public GameObject NonRotating
        {
            get
            {
                return wheel.nonRotating;
            }
            set
            {
                wheel.nonRotating = value;
            }
        }

        /// <summary>
        /// Returns velocity at the wheel's center position in [m/s].
        /// </summary>
        public Vector3 pointVelocity
        {
            get
            {
                return parentRigidbody.GetPointVelocity(wheel.worldPosition);
            }
        }

        /// <summary>
        /// Returns angular velocity of the wheel in radians. Multiply by wheel radius to get linear speed.
        /// </summary>
        public float angularVelocity
        {
            get
            {
                return wheel.angularVelocity;
            }
        }

        /// <summary>
        /// Layers that will be ignored when doing ground detection.
        /// </summary>
        public LayerMask ScanIgnoreLayers
        {
            get
            {
                return scanIgnoreLayers;
            }

            set
            {
                scanIgnoreLayers = value;
            }
        }

        #endregion
    }
}
