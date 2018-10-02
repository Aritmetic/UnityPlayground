using UnityEngine;
using System.Collections;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Everything related to steering and axle's geometry.
    /// </summary>
    [System.Serializable]
    public class Steering
    {
        /// <summary>
        /// Steering angle at low speeds.
        /// </summary>
        [Tooltip(" Steering angle at low speeds.")]
        [Range(0f, 60f)]
        [ShowInSettings(20f, 50f, 1f)]
        public float lowSpeedAngle = 30f;

        /// <summary>
        /// Steering angle at high speeds.
        /// </summary>
        [Tooltip(" Steering angle at high speeds.")]
        [Range(0f, 60f)]
        [ShowInSettings(5f, 20f, 1f)]
        public float highSpeedAngle = 14f;

        /// <summary>
        /// Speed after which only high speed angle will be used. Also affects dynamic smoothing.
        /// </summary>
        [Tooltip("Speed after which only high speed angle will be used. Also affects dynamic smoothing.")]
        public float crossoverSpeed = 35f;

        /// <summary>
        /// Only used if limitSteeringRate is true. Will limit wheels so that they can only steer up to the set degree 
        /// limit per second. E.g. 60 degrees per second will mean that the wheels that have 30 degree steer angle will
        /// take 1 second to steer from full left to full right.
        /// </summary>
        [Tooltip("Only used if limitSteeringRate is true.Will limit wheels so that they can only steer up to the set degree" +
            "limit per second. E.g. 60 degrees per second will mean that the wheels that have 30 degree steer angle will" +
            "take 1 second to steer from full left to full right.")]
        [Range(0f, 360f)]
        [ShowInSettings(50f, 350f, 25f)]
        public float degreesPerSecondLimit = 250f;

        /// <summary>
        /// Steer angle will be multiplied by this value to get steering wheel angle. Ignored if steering wheel is null.
        /// </summary>
        [Tooltip("Steer angle will be multiplied by this value to get steering wheel angle.")]
        public float steeringWheelTurnRatio = 2f;

        /// <summary>
        /// [Optional] Steering wheel game object. 
        /// </summary>
        [Tooltip("[Optional] Steering wheel game object.")]
        public GameObject steeringWheel;

        /// <summary>
        /// Intensity of braking used when steering a tracked vehicle. Percentage of the max brake torque.
        /// Set to 1 for 100% braking torque when using steer. Higer value will make the tracked vehicle
        /// turn tighter but will slow it down more.
        /// </summary>
        [Range(0f, 1f)]
        public float trackedSteerIntensity = 1f;

        private float angle;
        private Vector3 initialSteeringWheelRotation;
        private float targetAngle;
        private VehicleController vc;

        /// <summary>
        /// Current steer angle.
        /// </summary>
        public float Angle
        {
            get
            {
                return angle;
            }
        }

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
            if(steeringWheel != null)
            {
                initialSteeringWheelRotation = steeringWheel.transform.localRotation.eulerAngles;
            }
        }

        public void Steer()
        {
            float maxAngle = Mathf.Abs(Mathf.Lerp(lowSpeedAngle, highSpeedAngle, vc.Speed / crossoverSpeed));
            targetAngle = maxAngle * vc.input.Horizontal;
            angle = Mathf.MoveTowards(angle, targetAngle, degreesPerSecondLimit * Time.fixedDeltaTime);

            foreach (Axle axle in vc.axles)
            {
                float axleAngle = angle * axle.geometry.steerCoefficient;

                // Apply Ackermann
                if (angle < 0) //steering left
                {
                    axle.leftWheel.SteerAngle = axleAngle;
                    axle.rightWheel.SteerAngle = axleAngle - (axleAngle * axle.geometry.ackermannPercent);
                }
                else if (angle > 0) //steering right
                {
                    axle.leftWheel.SteerAngle = axleAngle - (axleAngle * axle.geometry.ackermannPercent);
                    axle.rightWheel.SteerAngle = axleAngle;
                }
                else // steering straight
                {
                    axle.leftWheel.SteerAngle = axleAngle;
                    axle.rightWheel.SteerAngle = axleAngle;
                }

                // Detoriate handling when damaged
                if(vc.damage.enabled && vc.damage.performanceDegradation)
                {
                    axle.leftWheel.SteerAngle += (axle.leftWheel.Damage / vc.damage.allowedDamage) * maxAngle * axle.leftWheel.DamageSteerDirection;
                    axle.rightWheel.SteerAngle += (axle.rightWheel.Damage / vc.damage.allowedDamage) * maxAngle * axle.rightWheel.DamageSteerDirection;
                }

                // Tracked steering
                if(vc.tracks.trackedVehicle)
                {
                    float brakePercent = Mathf.Abs(angle) / maxAngle;
                    brakePercent *= (0.5f + (1f - vc.input.Vertical) * 0.5f) * vc.steering.trackedSteerIntensity;

                    if (angle < 0)
                    {
                        axle.leftWheel.SetBrakeIntensity(brakePercent);
                        axle.rightWheel.MotorTorque += axle.leftWheel.MotorTorque;
                        axle.leftWheel.MotorTorque = 0f;
                    }
                    else if (angle > 0)
                    {
                        axle.rightWheel.SetBrakeIntensity(brakePercent);
                        axle.leftWheel.MotorTorque += axle.rightWheel.MotorTorque;
                        axle.rightWheel.MotorTorque = 0f;
                    }
                }
            }

            // Adjust steering wheel object if it exists
            if(steeringWheel != null)
            {
                float wheelAngle = angle * steeringWheelTurnRatio;
                steeringWheel.transform.localRotation = Quaternion.Euler(initialSteeringWheelRotation);
                steeringWheel.transform.Rotate(Vector3.forward, wheelAngle);
            }
        }

        public void AdjustGeometry()
        {
            foreach(Axle axle in vc.axles)
            {
                axle.leftWheel.ControllerTransform.localEulerAngles = new Vector3(
                    -axle.geometry.casterAngle,
                    axle.geometry.toeAngle,
                    axle.leftWheel.ControllerTransform.localEulerAngles.z);
                axle.rightWheel.ControllerTransform.localEulerAngles = new Vector3(
                    -axle.geometry.casterAngle,
                    -axle.geometry.toeAngle,
                    axle.rightWheel.ControllerTransform.localEulerAngles.z);
            }
        }
    }
}

