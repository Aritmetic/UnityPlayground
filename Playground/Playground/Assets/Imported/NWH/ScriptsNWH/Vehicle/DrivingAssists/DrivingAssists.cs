using UnityEngine;
using System.Collections;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Systems to help driver with vehicle control.
    /// </summary>
    [System.Serializable]
    public class DrivingAssists
    {
        /// <summary>
        /// Base class for driving aids.
        /// </summary>
        [System.Serializable]
        public class DrivingAid
        {
            /// <summary>
            /// Determines if driving aid should be used.
            /// </summary>
            [Tooltip("Determines if driving aid should be used.")]
            [ShowInTelemetry, ShowInSettings]
            public bool enabled = false;

            /// <summary>
            /// Is driving aid currently active?
            /// </summary>
            [ShowInTelemetry]
            [HideInInspector]
            public bool active;

            /// <summary>
            /// Higher intensity will result in driving aid affecting the vehicle behavior stronger.
            /// </summary>
            [Tooltip("Higher intensity will result in driving aid affecting the vehicle behavior stronger.")]
            [ShowInTelemetry, ShowInSettings(0f, 1f, 0.1f)]
            [Range(0, 1)]
            public float intensity = 0.3f;
        }

        /// <summary>
        /// Anti brake lock system.
        /// </summary>
        [Tooltip("Anti brake lock system.")]
        [SerializeField]
        public ABS abs = new ABS();

        /// <summary>
        /// Traction control.
        /// </summary>
        [Tooltip("Traction control system.")]
        [SerializeField]
        public TractionControl tcs = new TractionControl();

        /// <summary>
        /// Stability help. It will improve vehicle handling so should not be used as an option in competitive games but rather
        /// to achieve more arcade-like behavior when needed. Force is applied that is not dependent on slip so vehicle will be able to 
        /// steer even when skidding.
        /// </summary>
        [Tooltip("Stability assist system.")]
        [SerializeField]
        public Stability stability = new Stability();

        /// <summary>
        /// Setting other than 0 will add forces that act as to prevent vehicle from spinning out when drifting.
        /// Effect does not work at low speeds to allow for doughnuts.
        /// </summary>
        [Tooltip("Setting other than 0 will add forces that act as to prevent vehicle from spinning out when drifting." +
            "Effect does not work at low speeds to allow for doughnuts.")]
        [SerializeField]
        public DriftAssist driftAssist = new DriftAssist();

        private VehicleController vc;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

        public void Update()
        {
            if (abs.enabled) abs.Update(vc);
            if (tcs.enabled) tcs.Update(vc);
            if (stability.enabled) stability.Update(vc);
            if (driftAssist.enabled) driftAssist.Update(vc);
        }
    }
}

