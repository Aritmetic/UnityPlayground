using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.VehiclePhysics 
{
    /// <summary>
    /// Represents a single axle of a vehicle.
    /// </summary>
    [System.Serializable]
	public class Axle
	{
        /// <summary>
        /// Axle's left wheel.
        /// </summary>
        [Tooltip("Axle's left wheel.")]
		public Wheel leftWheel = new Wheel();

        /// <summary>
        /// Axle's right wheel.
        /// </summary>
        [Tooltip("Axle's right wheel.")]
        public Wheel rightWheel = new Wheel();

        /// <summary>
        /// Amount of torque that the axle will receive where 1 equals all the available torque for that axle.
        /// </summary>
        [ShowInTelemetry]
        private float bias;

        /// <summary>
        /// Class holding all geometry related data for axle and it's wheels.
        /// </summary>
        [System.Serializable]
        public class Geometry
        {
            [Tooltip("Determines what percentage of the steer angle will be applied to the wheel. If set to negative value" +
                " wheels will turn in direction opposite of input.")]
            [Range(-1f, 1f)]
            [ShowInTelemetry, ShowInSettings(-1f, 1f, 0.1f)]
            public float steerCoefficient;

            [Tooltip("Set to positive for Pro-Ackerman steering (inner wheel steers more) or to negative for Anti-Ackerman steering.")]
            [Range(-1f, 1f)]
            [ShowInTelemetry, ShowInSettings(-0.4f, 0.4f, 0.05f)]
            public float ackermannPercent = 0.15f;

            [Tooltip("Positive toe angle means that the wheels will face inwards (front of the wheel angled toward longitudinal center of the vehicle).")]
            [Range(-8f, 8f)]
            [ShowInTelemetry, ShowInSettings(-8f, 8f, 0.5f)]
            public float toeAngle = 0;

            [Tooltip("Positive caster means that whe wheel will be angled towards the front of the vehicle while negative " +
                " caster will angle the wheel in opposite direction (shopping cart wheel).")]
            [Range(-8f, 8f)]
            [ShowInTelemetry, ShowInSettings(-8f, 8f, 0.5f)]
            public float casterAngle = 0;

            [Tooltip("Camber at the top of the spring travel (wheel is at the highest point). Set to other than 0 to override WC3D's settings," +
                "and set to 0 if you want to use camber settings and curve from WC3D inpector.")]
            [Range(-10f, 10f)]
            [ShowInSettings(-10f, 10f, 1f)]
            public float camberAtTop = 0;

            [Tooltip("Camber at the bottom of the spring travel (wheel is at the lowest point).")]
            [Range(-10f, 10f)]
            [ShowInSettings(-10f, 10f, 1f)]
            public float camberAtBottom = 0;

            [Tooltip("Setting to true will override camber settings and camber will be calculated from position of the (imaginary) axle object instead.")]
            public bool isSolid = false;

            [Tooltip("Used to reduce roll in the vehicle. Should not exceed max spring force setting. Another way to reduce roll is to" +
                " adjust center of mass to be lower.")]
            [ShowInTelemetry, ShowInSettings(0f, 20000f, 1000f)]
            public float antiRollBarForce;
        }

        [Tooltip("Geometry related parameters.")]
        [SerializeField]
        public Geometry geometry = new Geometry();

        /// <summary>
        /// Amount of power that the axle will receive shown as a ratio. If two axles have both power coefficient of 1 each will receive half
        /// of total power (1:1), if first axle has p.c. of 1 and rear has p.c. of 0.5, this means that first axle will receive
        /// (1 / (1 + 0.5)) = 0.66 (66%) of total power and rear will receive (0.5 / (1 + 0.5)) = 0.33 (33%) of total power.
        /// </summary>
        [Tooltip("Amount of power that the axle will receive shown as a ratio. If two axles have both power coefficient of 1 each will receive half" +
            " of total power (1:1), if first axle has p.c. of 1 and rear has p.c. of 0.5, this means that first axle will receive" +
            " (1 / (1 + 0.5)) = 0.66 (66%) of total power and rear will receive (0.5 / (1 + 0.5)) = 0.33 (33%) of total power.")]           
        [Range(0f, 1f)]
        [ShowInTelemetry, ShowInSettings(0f, 1f, 0.1f)]
        public float powerCoefficient = 1f;

        /// <summary>
        /// If set to 1 axle will receive full brake torque as set by Max Torque parameter under Brake section while 0 
        /// means no breaking at all.
        /// </summary>
        [Tooltip("If set to 1 axle will receive full brake torque as set by Max Torque parameter under Brake section while " +
            "0 means no breaking at all.")]
        [Range(0f, 1f)]
        [ShowInTelemetry, ShowInSettings(0f, 1f, 0.1f)]
        public float brakeCoefficient = 1f;

        /// <summary>
        /// If set to 1 axle will receive full brake torque when handbrake is used.
        /// </summary>
        [Tooltip("If set to 1 axle will receive full brake torque when handbrake is used.")]
        [Range(0f, 1f)]
        [ShowInTelemetry, ShowInSettings(0f, 1f, 0.1f)]
        public float handbrakeCoefficient;     
        
        /// <summary>
        /// Axle differential. 
        /// Equal - torque will be split equally between wheels at all times.
        /// Open - faster spinning wheel will receive more torque.
        /// Limited Slip - both wheels will always get some torque, depends on RPM of each wheel.
        /// Locking - slower spinning wheel will receive most torque.
        /// </summary>
        public enum DifferentialType { Equal, Open, LimitedSlip, Locking }

        /// <summary>
        /// Strength of the axle differential. Affects LimitedSlip and Locking differentials.
        /// </summary>
        [Tooltip("Strength of the axle differential. Affects LimitedSlip and Locking differentials.")]
        [Range(0f, 1f)]
        [ShowInTelemetry, ShowInSettings(0f, 1f, 0.1f)]
        public float differentialStrength;

        /// <summary>
        /// Type of differential to be used when splitting torque between left and right wheel.
        /// </summary>
        [Tooltip("Type of differential to be used when splitting torque between left and right wheel.")]
        [ShowInTelemetry]
        public DifferentialType differentialType = DifferentialType.LimitedSlip;

		private VehicleController vc;

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
        /// Returns true if axle can receive power / torque.
        /// </summary>
        [ShowInTelemetry]
        public bool IsPowered
        {
            get
            {
                return powerCoefficient > 0 ? true : false;
            }
        }

        /// <summary>
        /// RPM of the axle as an average between wheels.
        /// </summary>
        [ShowInTelemetry]
        public float RPM
        {
            get 
            {
                return (leftWheel.RPM + rightWheel.RPM) / 2f;
            }
        }

        /// <summary>
        /// Smoothed RPM of the axle.
        /// </summary>
        public float SmoothRPM
        {
            get
            {
                return (leftWheel.SmoothRPM + rightWheel.SmoothRPM) / 2f;
            }
        }

        /// <summary>
        /// RPM of the axle as if the wheels are not slipping.
        /// </summary>
        public float NoSlipRPM
        {
            get
            {
                return (leftWheel.NoSlipRPM + rightWheel.NoSlipRPM) / 2f;
            }
        }

        /// <summary>
        /// True if there is longitudinal slip on axles's left or right wheel.
        /// </summary>
        [ShowInTelemetry]
        public bool WheelSpin
        {
            get
            {
                return (leftWheel.HasForwardSlip || rightWheel.HasForwardSlip) && IsPowered;
            }
        }

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
            leftWheel.Initialize(vc);
            rightWheel.Initialize(vc);

            leftWheel.brakeCoefficient = brakeCoefficient;
            rightWheel.brakeCoefficient = brakeCoefficient;
        }

        public void Update()
        {
            // Set camber only if it has not already been set by the WC3D's inspector and if axle is not solid (in which case camber is set automatically).
            if (!(geometry.isSolid || (geometry.camberAtBottom == 0 && geometry.camberAtTop == 0)))
            {
                leftWheel.WheelController.SetCamber(geometry.camberAtTop, geometry.camberAtBottom);
                rightWheel.WheelController.SetCamber(geometry.camberAtTop, geometry.camberAtBottom);
            }

            // Apply anti roll bar
            if (geometry.antiRollBarForce != 0)
            {
                float leftTravel = leftWheel.SpringTravel;
                float rightTravel = rightWheel.SpringTravel;

                // Anti-roll bar is linear to prevent possible jitter at lower update rates.
                float arf = (leftTravel - rightTravel) * geometry.antiRollBarForce;

                if (leftWheel.IsGrounded && rightWheel.IsGrounded)
                {
                    vc.vehicleRigidbody.AddForceAtPosition(leftWheel.ControllerTransform.up * -arf, leftWheel.ControllerTransform.position);
                    vc.vehicleRigidbody.AddForceAtPosition(rightWheel.ControllerTransform.up * arf, rightWheel.ControllerTransform.position);
                }
            }

            // Calculate camber for solid axle
            if(geometry.isSolid)
            {
                // Center point of imaginary axle
                Vector3 position = (leftWheel.WheelController.springTravelPoint + rightWheel.WheelController.springTravelPoint) / 2f;
                Vector3 direction = position - leftWheel.WheelController.springTravelPoint;

                // Calculate camber from the mid point
                float camberAngle = VehicleController.AngleSigned(vc.transform.right, direction, vc.transform.forward);
                camberAngle = Mathf.Clamp(camberAngle, -25f, 25f);

                // Set camber
                leftWheel.WheelController.SetCamber(camberAngle);
                rightWheel.WheelController.SetCamber(-camberAngle);
                geometry.camberAtBottom = geometry.camberAtTop = camberAngle;
            }
        }

        /// <summary>
        /// Splits torque between left and right wheel according to the differential setting.
        /// Not used with tracked vehicles.
        /// </summary>
        /// <param name="torque">Amount of torque that will be split.</param>
        /// <param name="topRPM">Maximim RPM that axle is currently allowed to have.</param>
        public void TorqueSplit(float torque, float topRPM)
        {
            if(!vc.tracks.trackedVehicle)
            {
                float leftRPM = Mathf.Abs(leftWheel.SmoothRPM);
                float rightRPM = Mathf.Abs(rightWheel.SmoothRPM);

                float rpmSum = Mathf.Abs(leftRPM) + Mathf.Abs(rightRPM);

                if (rpmSum != 0)
                {
                    // Equal differential - both wheels always get same torque.
                    if (differentialType == DifferentialType.Equal)
                    {
                        leftWheel.Bias = rightWheel.Bias = 0.5f;
                    }
                    // Open differential - wheel with higher RPM gets more torque.
                    else if (differentialType == DifferentialType.Open)
                    {
                        leftWheel.Bias = (leftRPM / rpmSum);
                        rightWheel.Bias = (rightRPM / rpmSum);
                    }
                    // Limited slip and locking differentail - wheel with lower RPM gets more torque.
                    else if (differentialType == DifferentialType.LimitedSlip || differentialType == DifferentialType.Locking)
                    {
                        // Calculate torque split for limited slip differential.
                        leftWheel.Bias = Mathf.Pow(1f - (leftRPM / rpmSum), 2f);
                        rightWheel.Bias = Mathf.Pow(1f - (rightRPM / rpmSum), 2f);

                        // Tighten torque split for locking differential.
                        if (differentialType == DifferentialType.Locking)
                        {
                            leftWheel.Bias = Mathf.Pow(leftWheel.Bias, 6f);
                            rightWheel.Bias = Mathf.Pow(rightWheel.Bias, 6f);
                        }

                        float biasSum = leftWheel.Bias + rightWheel.Bias;
                        leftWheel.Bias = leftWheel.Bias / biasSum;
                        rightWheel.Bias = rightWheel.Bias / biasSum;

                        // Adjust strength of differential
                        if (leftWheel.Bias < rightWheel.Bias)
                        {
                            leftWheel.Bias = Mathf.Lerp(leftWheel.Bias, 1f - leftWheel.Bias, 1f - differentialStrength);
                            rightWheel.Bias = 1f - leftWheel.Bias;
                        }
                        else
                        {
                            rightWheel.Bias = Mathf.Lerp(rightWheel.Bias, 1f - rightWheel.Bias, 1f - differentialStrength);
                            leftWheel.Bias = 1f - rightWheel.Bias;
                        }
                    }
                }
                // RPM is 0 and split cannot be calculated. Split equally.
                else
                {
                    leftWheel.Bias = rightWheel.Bias = 0.5f;
                }

                // RPM of the axle is higher than the RPM received from transmission, stop delivering torque.
                if (SmoothRPM > topRPM * 2.2f)
                {
                    leftWheel.Bias = rightWheel.Bias = 0;
                }

                leftWheel.MotorTorque = torque * leftWheel.Bias;
                rightWheel.MotorTorque = torque * rightWheel.Bias;
            }
        }
	}
}

