using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NWH.WheelController3D;

namespace NWH.VehiclePhysics 
{
    /// <summary>
    /// Contains everything related to wheels. To access WC3D's properties directly use WheelController getter.
    /// Most used variables are wrapped in getters and setter to enable eventual (but unlikely) future compatibility 
    /// with default wheel collider.
    /// Wheel class is not equal to WheelController class. To access WC3D (WheelController) use WheelController getter/setter.
    /// </summary>
    [System.Serializable]
	public class Wheel
	{
        public Wheel() { }

        public Wheel(WheelController wc, VehicleController vc)
        {
            this.wheelController = wc;
            this.vc = vc;
        }

        /// <summary>
        /// Amount of brake torque wheel will receive as a percentage from max brake torque.
        /// </summary>
        [HideInInspector]
        public float brakeCoefficient = 1f;

        [Tooltip("Object containing WheelController component. Using Unity's WheelCollider is not supported.")]
        [SerializeField]
		public WheelController wheelController;

        private VehicleController vc;
        private float smoothRPM;
        private float prevSmoothRPM;
        [ShowInTelemetry]
        private float bias;
        private float damage;
        private float damageSteerDirection;
        private bool singleRayByDefault;
        private float prevSideSlip;
        private float prevForwardSlip;
        private float smoothForwardSlip;
        private float smoothSideSlip;

        public void Update()
        {
            // Calculate smooth RPM
            prevSmoothRPM = smoothRPM;
            float a = (smoothRPM - prevSmoothRPM) / Time.fixedDeltaTime;
            smoothRPM = Mathf.Lerp(smoothRPM, wheelController.rpm, Time.fixedDeltaTime * 20f)
                                + (smoothRPM - prevSmoothRPM) * 50 + a * Time.fixedDeltaTime * Time.fixedDeltaTime;

            // Calculate smooth forward slip
            smoothForwardSlip = Mathf.Lerp(smoothForwardSlip, ForwardSlip, Time.fixedDeltaTime * 5f);

            // Calculate smooth side slip
            smoothSideSlip = Mathf.Lerp(smoothSideSlip, SideSlip, Time.fixedDeltaTime * 5f);
        }


        /// <summary>
        /// Amount of motor torque this wheel will receive as a percentage from total torque on the axle.
        /// </summary>
        public float Bias
        {
            get
            {
                return bias;
            }
            set
            {
                bias = Mathf.Clamp01(value);
            }
        }

        /// <summary>
        /// Longitudinal slip of the wheel.
        /// </summary>
        [ShowInTelemetry]
        public float ForwardSlip
		{
			get
			{
                return Mathf.Clamp01(Mathf.Abs(wheelController.forwardFriction.slip));
			}
		}

        /// <summary>
        /// Lateral slip of the wheel.
        /// </summary>
        [ShowInTelemetry]
        public float SideSlip
		{
			get
			{
				return Mathf.Clamp01(Mathf.Abs(wheelController.sideFriction.slip));
			}
		}

        /// <summary>
        /// Smoothed longitudinal slip of the wheel for use in effects.
        /// </summary>
        public float SmoothForwardSlip
        {
            get
            {
                return smoothForwardSlip;
            }
        }

        /// <summary>
        /// Smoothed lateral slip of the wheel for use in effects.
        /// </summary>
        public float SmoothSideSlip
        {
            get
            {
                return smoothSideSlip;
            }
        }

        /// <summary>
        /// Longitudinal slip percentage where 1 represents slip equal to forward slip threshold. 
        /// </summary>
        public float ForwardSlipPercent
        {
            get
            {
                return Mathf.Clamp01(ForwardSlip / vc.forwardSlipThreshold);
            }
        }

        /// <summary>
        /// Lateral slip percentage where 1 represents slip equal to side slip threshold. 
        /// </summary>
        public float SideSlipPercent
        {
            get
            {
                return Mathf.Clamp01(SideSlip / vc.sideSlipThreshold);
            }
        }

        /// <summary>
        /// True if longitudinal slip is larget than forward slip threshold.
        /// </summary>
        public bool HasForwardSlip
        {
            get
            {
                if (Mathf.Abs(ForwardSlip) > vc.forwardSlipThreshold && Mathf.Abs(wheelController.rpm) > 6f)
                    return true;
                else
                    return false;
            }         
        }

        /// <summary>
        /// Returns ground entity the wheel is currently on.
        /// </summary>
        public GroundDetection.GroundEntity CurrentGroundEntity
        {
            get
            {
                return vc.groundDetection.GetCurrentGroundEntity(this.wheelController);
            }
        }

        /// <summary>
        /// Returns the name of the ground entity the wheel is currently on.
        /// </summary>
        [ShowInTelemetry]
        public string CurrentGroundEntityName
        {
            get
            {
                return vc.groundDetection.GetCurrentGroundEntity(this.wheelController).name;
            }
        }

        /// <summary>
        /// True if lateral slip is larger than side slip threshold.
        /// </summary>
        public bool HasSideSlip
        {
            get
            {
                if (Mathf.Abs(SideSlip) > vc.sideSlipThreshold)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Damage that the wheel has suffered so far.
        /// </summary>
        public float Damage
        {
            get { return damage; }
            set { damage = value; }
        }

        /// <summary>
        /// RPM of the wheel. In most cases it is better to use SmoothRPM instead.
        /// </summary>
        [ShowInTelemetry]
        public float RPM
		{
			get
			{
			    return wheelController.rpm;
			}
		}

        /// <summary>
        /// Transform to which WheelController component is attached.
        /// </summary>
        public Transform ControllerTransform
        {
            get
            {
                return wheelController.transform;
            }
        }

        /// <summary>
        /// True if wheel is touching any object.
        /// </summary>
        [ShowInTelemetry]
        public bool IsGrounded
        {
            get
            {
                return wheelController.isGrounded;
            }
        }

        /// <summary>
        /// Distance from top to bottom of spring travel.
        /// </summary>
        public float SpringTravel
        {
            get
            {
                return wheelController.springCompression * wheelController.springLength;
            }
        }

        /// <summary>
        /// Torque in Nm used to accelerate the wheel.
        /// </summary>
        [ShowInTelemetry]
        public float MotorTorque
        {
            get
            {
                return wheelController.motorTorque;
            }
            set
            {
                wheelController.motorTorque = value;
            }
        }

        /// <summary>
        /// Torque in Nm used to slow down the wheel.
        /// </summary>
        [ShowInTelemetry]
        public float BrakeTorque
        {
            get
            {
                return wheelController.brakeTorque;
            }
            set
            {
                wheelController.brakeTorque = value;
            }
        }

        /// <summary>
        /// Transform of the object containing mesh(es) representing the wheel.
        /// </summary>
        public Transform VisualTransform
        {
            get
            {
                return wheelController.Visual.transform;
            }
        }

        /// <summary>
        /// Wheel radius.
        /// </summary>
        public float Radius
        {
            get
            {
                return wheelController.tireRadius;
            }
        }

        /// <summary>
        /// Wheel width.
        /// </summary>
        public float Width
        {
            get
            {
                return wheelController.tireWidth;
            }
        }

        /// <summary>
        /// GameObject cointaining WheelController component.
        /// </summary>
        public GameObject ControllerGO
        {
            get
            {
                return wheelController.gameObject;
            }
        }

        /// <summary>
        /// Steer angle of the wheel in degrees.
        /// </summary>
        public float SteerAngle
        {
            get
            {
                return wheelController.steerAngle;
            }
            set
            {
                wheelController.steerAngle = value;
            }
        }

        /// <summary>
        /// Smoothed RPM of the wheel. Should be used instead of the actual RPM for most calculations.
        /// </summary>
        public float SmoothRPM
        {
            get 
            {
                return smoothRPM;
            }
        }

        /// <summary>
        /// RPM of the wheel without slipping.
        /// </summary>
        public float NoSlipRPM
        {
            get
            {
                return wheelController.forwardFriction.speed / (6.28f * Radius);
            }
        }

        /// <summary>
        /// WheelController (WC3D) of the wheel.
        /// </summary>
        public WheelController WheelController
        {
            get
            {
                return wheelController;
            }
        }

        /// <summary>
        /// Random steer direction of a damaged wheel. Depending on the amount of the damage vehicle has received this
        /// value will be multiplied by the steer angle making the wheel gradually point more and more in a random direction
        /// drastically worsening the handling.
        /// </summary>
        public float DamageSteerDirection
        {
            get
            {
                return damageSteerDirection;
            }
        }

        /// <summary>
        /// Adds brake torque to the wheel on top of the existing torque. Value is clamped to max brake torque.
        /// </summary>
        /// <param name="torque">Torque in Nm that will be applied to the wheel to slow it down.</param>
        public void AddBrakeTorque(float torque)
        {
            torque *= brakeCoefficient;
            if (torque < 0)
                wheelController.brakeTorque += 0f;
            else
				wheelController.brakeTorque += torque;

            if (wheelController.brakeTorque > vc.brakes.maxTorque)
                wheelController.brakeTorque = vc.brakes.maxTorque;

            if (wheelController.brakeTorque < 0)
                wheelController.brakeTorque = 0;

            vc.brakes.Active = true;
        }

        /// <summary>
        /// Applies very high braking torque to the wheel locking it up. Unlike other methods not limited by max brake torque.
        /// </summary>
        public void Lockup()
        {
            wheelController.brakeTorque = 1000000f;
        }

		public void Initialize(VehicleController vc)
		{
			this.vc = vc;
            damageSteerDirection = Random.Range(-1f, 1f);
            singleRayByDefault = WheelController.singleRay;
		}

        /// <summary>
        /// Adds brake torque as a percentage in range from 0 to 1.
        /// </summary>
		public void SetBrakeIntensity(float percent)
		{
            AddBrakeTorque(Mathf.Abs(vc.brakes.maxTorque * Mathf.Clamp01(Mathf.Abs(percent))));
		}

        /// <summary>
        /// Sets brake torque to the provided value. Use 0 to remove any braking.
        /// </summary>
        public void ResetBrakes(float value)
        {
            wheelController.brakeTorque = Mathf.Abs(value);
        }

        /// <summary>
        /// Activates the wheel after it has been suspended by turning off single ray mode. If the wheel is
        /// in single ray mode by default it will be left on.
        /// </summary>
        public void Activate()
        {
            if(!singleRayByDefault)
                wheelController.singleRay = false;
        }

        /// <summary>
        /// Turns on single ray mode to prevent unnecessary raycasting for inactive wheels / vehicles.
        /// </summary>
        public void Suspend()
        {
            wheelController.singleRay = true;
        }
	}
}

