using UnityEngine;
using System.Collections;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Supercharger, turbocharger, etc. Can also be used on vehicles with no forced induction for sound effects such as
    /// intake noise or engine fan noise.
    /// </summary>
    [System.Serializable]
    public class ForcedInduction
    {
        /// <summary>
        /// Should forced induction be used?
        /// </summary>
        [Tooltip("Should forced induction be used?")]
        [ShowInTelemetry, ShowInSettings]
        public bool useForcedInduction = true;

        /// <summary>
        /// Shortest time possible needed for turbo to spool up to its maximum RPM.
        /// </summary>
        [Tooltip("Shortest time possible needed for turbo to spool up to its maximum RPM.")]
        [ShowInSettings(0.1f, 2f, 0.1f)]
        public float spoolUpTime = 1f;

        /// <summary>
        /// Additional power that will be added to the engine's power. 
        /// This is the maximum value possible and depends on spool percent.
        /// </summary>
        [Tooltip("Additional power percent of engine's max power that will be added to the engine's power.")]
        [ShowInSettings(0.1f, 1f, 0.1f)]
        public float maxPowerGainMultiplier = 0.4f;

        /// <summary>
        /// Flutter can sound when true.
        /// </summary>
        [HideInInspector]
        public bool flutterSoundFlag = false;

        private float maxRPM = 120000;
        private float spoolVelocity;
        private float rpm;
        private int prevGear;
        private float prevEngineRPMPercent;
        private float prevVerticalInput;

        private VehicleController vc;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

        public void Update()
        {
            if(useForcedInduction)
            {
                float t = prevEngineRPMPercent > vc.engine.RPMPercent ? spoolUpTime * 0.4f : spoolUpTime;

                float targetRpm = (vc.engine.RPMPercent * maxRPM) * 0.3f + (maxRPM * vc.Load) * 0.7f;
                rpm = Mathf.SmoothDamp(rpm, targetRpm, ref spoolVelocity, t);
                rpm = Mathf.Clamp(rpm, 0f, maxRPM);

                // Flutter
                if ((prevGear != vc.transmission.Gear && vc.transmission.Gear > 1) || vc.input.Vertical < 0.3f && prevVerticalInput > 0.7f)
                {
                    flutterSoundFlag = true;
                    targetRpm = targetRpm / 2f;
                }
            }
            prevGear = vc.transmission.Gear;
            prevEngineRPMPercent = vc.engine.RPMPercent;
            prevVerticalInput = vc.input.Vertical;
        }

        /// <summary>
        /// Percent of forced induction's RPM in relation to its max RPM.
        /// </summary>
        [ShowInTelemetry]
        public float SpoolPercent
        {
            get
            {
                return rpm / maxRPM;
            }
        }

        /// <summary>
        /// Current power gained from forced induction.
        /// </summary>
        [ShowInTelemetry]
        public float PowerGainMultiplier
        {
            get
            {
               if (!useForcedInduction)
                    return 1f;
               else
                    return 1f + ((rpm / maxRPM) * maxPowerGainMultiplier);
            }
        }
    }
}
