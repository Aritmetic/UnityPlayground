using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Class for storing input states of the vehicle. 
    /// </summary>
    [HideInInspector]
    [System.Serializable]
	public class InputStates
	{
        /// <summary>
        /// Horizontal axis. Used for steering.
        /// </summary>
        private float horizontal;

        /// <summary>
        /// Vertical axis. Used for accelerating and braking.
        /// </summary>
        private float vertical;

        private float clutch;
        private float handbrake;

        // Shift up and down flags. These will stay true until manual shift function is called.
        private bool shiftUp;
        private bool shiftDown;

        // Lights
        // Turning on one blinker will cancel out the other.
        public bool leftBlinker;
        public bool rightBlinker;
        public bool lowBeamLights;
        public bool fullBeamLights;
        public bool hazardLights;

        // Trailer
        /// <summary>
        /// Trailer will be attached only if under the threshold set in trailer options.
        /// </summary>
        public bool trailerAttachDetach;

        public bool flipOver;

        // Horn
        public bool horn;

        private VehicleController vc;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

		public bool ShiftUp
        {
            get
            {
                if (!vc.Active) return false;
                return shiftUp;
            }
            set
            {
                shiftUp = value;
            }
		}

		public bool ShiftDown
		{
			get
			{
                if (!vc.Active) return false;
                return shiftDown;
			}
            set
            {
                shiftDown = value;
            }
		}

        public float Horizontal
        {
            get
            {
                if (!vc.Active) return 0;
                return horizontal;
            }

            set
            {
                horizontal = value;
            }
        }

        /// <summary>
        /// Returns vertical input without any processing.
        /// </summary>
        public float RawVertical
        {
            get
            {
                if (!vc.Active) return 0;

                return vertical;
            }
        }

        public float Vertical
        {
            get
            {
                // Vehicle not active, return 0
                if (!vc.Active) return 0;

                // If tracked add vertical input when turning as tank requires power to turn
                if (vc.tracks.trackedVehicle) return Mathf.Clamp(vertical + Mathf.Sign(vertical) * Mathf.Abs(horizontal), -1f, 1f);

                return vertical;
            }

            set
            {
                vertical = value;
            }
        }

        public float Clutch
        {
            get
            {
                return clutch;
            }
            set
            {
                clutch = Mathf.Clamp01(value);
            }
        }

        public float Handbrake
        {
            get
            {
                if (!vc.Active) return 0;
                return handbrake;
            }

            set
            {
                handbrake = Mathf.Clamp01(value);
            }
        }
    }
}
