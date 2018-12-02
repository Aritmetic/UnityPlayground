using UnityEngine;
using System.Collections;

namespace NWH.VehiclePhysics
{
    [System.Serializable]
    public class Brakes
    {
        /// <summary>
        /// Max brake torque that can be applied to each wheel. To adjust braking on per-axle basis change brake coefficients under Axle settings.
        /// </summary>
        [Tooltip("Max brake torque that can be applied to each wheel. " +
            "To adjust braking on per-axle basis change brake coefficients under Axle settings")]
        public float maxTorque = 5000f;

        /// <summary>
        /// Imitation of rolling resistance and friction between drivetrain parts. Applied to all wheels.
        /// </summary>
        [Tooltip("Imitation of rolling resistance and friction between drivetrain parts. Applied to all wheels.")]
        public float frictionTorque = 120f;

        /// <summary>
        /// Time in seconds needed to reach full braking torque.
        /// </summary>
        [Tooltip("Time in seconds needed to reach full braking torque.")]
        [Range(0f, 5f)]
        public float smoothing = 0.9f;

        /// <summary>
        /// If true vehicle will break when in neutral and no throttle is applied.
        /// </summary>
        [Tooltip("If true vehicle will break when in neutral and no throttle is applied.")]
        public bool brakeWhileIdle = true;

        /// <summary>
        /// Set to true to use the air brake sound effect.
        /// </summary>
        [Tooltip("Set to true to use the air brake sound effect. Does not affect braking performance otherwise.")]
        public bool airBrakes = false;

        [HideInInspector]
        public float airBrakePressure;

        private float intensity;
        private float intensityVelocity;
        private bool active = false;

        /// <summary>
        /// Retruns true if vehicle is currently braking. Will return true if there is ANY brake torque applied to the wheels.
        /// </summary>
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                active = value;
            }
        }

        public void Update(VehicleController vc)
        {
            if (vc.input == null) return;

            // Reset brakes for this frame
            foreach (Wheel wheel in vc.Wheels)
                wheel.ResetBrakes(0);

            // Add friction 
            if(vc.input.Vertical == 0)
                foreach (Wheel wheel in vc.Wheels)
                    wheel.AddBrakeTorque(frictionTorque);

            // Avoid applying too large brake torque under very low speeds to prevent overshoot when calculating wheel's angular velocity inside WC3D.
            float brakingIntensityModifier = Mathf.Clamp(vc.Speed * 50f, 0.25f, 1f);

            // Engine braking on too high RPM (e.g. improper downshift)
            float axleRpmSum = 0f;
            foreach(Axle axle in vc.axles)
            {
                axleRpmSum += axle.NoSlipRPM;
            }
            float rpmFromWheels = axleRpmSum / vc.axles.Count;
            rpmFromWheels = vc.transmission.ReverseTransmitRPM(rpmFromWheels);

            float maxAllowedSpeed = vc.transmission.GetMaxSpeedForGear(vc.transmission.Gear);
            if (vc.Speed > maxAllowedSpeed
                && vc.transmission.transmissionType == Transmission.TransmissionType.Manual
                && vc.transmission.Gear != 0)
            {
                foreach (Axle axle in vc.axles)
                {
                    // Only brake on powered axles as non-powered axles are not connected to the engine.
                    if(axle.IsPowered)
                    {
                        axle.leftWheel.ResetBrakes(axle.leftWheel.WheelController.MaxPutDownForce * 0.5f * axle.leftWheel.Radius * axle.powerCoefficient * brakingIntensityModifier);
                        axle.rightWheel.ResetBrakes(axle.rightWheel.WheelController.MaxPutDownForce * 0.5f * axle.rightWheel.Radius * axle.powerCoefficient * brakingIntensityModifier);
                    }
                }
            }

            // Handbrake
            if (vc.input.Handbrake != 0 && vc.Active)
            {
                foreach (Axle axle in vc.axles)
                {
                    if (axle.handbrakeCoefficient > 0)
                    {
                        axle.leftWheel.AddBrakeTorque(maxTorque * axle.handbrakeCoefficient * vc.input.Handbrake * brakingIntensityModifier);
                        axle.rightWheel.AddBrakeTorque(maxTorque * axle.handbrakeCoefficient * vc.input.Handbrake * brakingIntensityModifier);
                    }
                }
            }

            // Everything after this point will set brake flag to active and thus register as braking.
            active = false;

            // Brake when idle
            if (!vc.Active || (vc.Active && brakeWhileIdle && Mathf.Abs(vc.input.Vertical) < 0.05f && vc.transmission.Gear == 0 && vc.Speed < 0.1f))
            {
                foreach (Axle axle in vc.axles)
                {
                    axle.leftWheel.SetBrakeIntensity(1);
                    axle.rightWheel.SetBrakeIntensity(1);
                }
            }

            // Brake on reverse direction
            if (vc.transmission.transmissionType == Transmission.TransmissionType.Automatic || vc.transmission.transmissionType == Transmission.TransmissionType.AutomaticSequential)
            {
                if (Mathf.Abs(vc.input.Vertical) > 0.05f && 
                    (Mathf.Sign(vc.transmission.Gear) != Mathf.Sign(vc.input.Vertical) || Mathf.Sign(vc.ForwardVelocity) != Mathf.Sign(vc.input.Vertical)))
                {
                    foreach (Wheel wheel in vc.Wheels)
                    {
                        intensity = Mathf.SmoothDamp(intensity, Mathf.Abs(vc.input.Vertical), ref intensityVelocity, smoothing);
                        wheel.SetBrakeIntensity(intensity * brakingIntensityModifier);
                    }
                }
            }
            else if (vc.transmission.transmissionType == Transmission.TransmissionType.Manual)
            {
                if (vc.input.Vertical < 0
                    || Mathf.Sign(vc.input.Vertical) != Mathf.Sign(vc.ForwardVelocity * vc.transmission.GearRatio))
                {
                    if (vc.Speed > 0.05f)
                    {
                        foreach (Wheel wheel in vc.Wheels)
                        {
                            intensity = Mathf.SmoothDamp(intensity, Mathf.Abs(vc.input.Vertical), ref intensityVelocity, smoothing);
                            wheel.SetBrakeIntensity(intensity * brakingIntensityModifier);
                        }
                    }
                }
            }

            // Air brakes pressure used for sound effects
            if (airBrakes)
            {
                airBrakePressure += Time.fixedDeltaTime * 1f;
                airBrakePressure = Mathf.Clamp(airBrakePressure, 0f, 3f);
            }

            // Tracked, do not allow wheel lock
            if (vc.tracks.trackedVehicle)
            {
                foreach (Wheel wheel in vc.Wheels)
                {
                    float maxTorque = wheel.WheelController.MaxPutDownForce * wheel.Radius;
                    if (wheel.BrakeTorque > maxTorque)
                    {
                        wheel.BrakeTorque = maxTorque;
                    }
                    else if (wheel.BrakeTorque < -maxTorque)
                    {
                        wheel.BrakeTorque = -maxTorque;
                    }
                }
            }            
        }
    }
}

