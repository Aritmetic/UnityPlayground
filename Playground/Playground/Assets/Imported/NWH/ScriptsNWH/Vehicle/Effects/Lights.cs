using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Class for controlling all of the vehicle lights.
    /// </summary>
    [System.Serializable]
    public class Lights
    {
        /// <summary>
        /// Determines the state of all lights.
        /// </summary>
        [HideInInspector]
        public bool enabled = true;
        private bool prevEnabled = false;

        /// <summary>
        /// Single vehicle light.
        /// </summary>
        [System.Serializable]
        public class VehicleLight
        {
            protected bool active;

            /// <summary>
            /// List of light sources of any type.
            /// </summary>
            [Tooltip("List of light sources of any type (point, spot, etc.).")]
            public List<Light> lightSources = new List<Light>();

            /// <summary>
            /// List of mesh renderers with standard shader. Emission will be turned on or off depending on light state.
            /// </summary>
            [Tooltip("List of mesh renderers with standard shader. Emission will be turned on or off depending on light state.")]
            public List<MeshRenderer> lightMeshRenderers = new List<MeshRenderer>();

            /// <summary>
            /// State of the light.
            /// </summary>
            public bool On { get { return active; } }

            /// <summary>
            /// Turns on the light source or enables emission on the mesh. Mesh is required to have standard shader.
            /// </summary>
            public void TurnOn()
            {
                active = true;

                foreach (Light light in lightSources)
                {
                    light.enabled = true;
                }

                foreach (MeshRenderer mr in lightMeshRenderers)
                {
                    mr.material.EnableKeyword("_EMISSION");
                }
            }

            /// <summary>
            /// Turns off the light source or disables emission on the mesh. Mesh is required to have standard shader.
            /// </summary>
            public void TurnOff()
            {
                active = false;

                foreach(Light light in lightSources)
                {
                    light.enabled = false;
                }

                foreach (MeshRenderer mr in lightMeshRenderers)
                {
                    mr.material.DisableKeyword("_EMISSION");
                }
            }
        }

        /// <summary>
        /// Rear lights that will light up when brake is pressed. Always red.
        /// </summary>
        [Tooltip("Rear lights that will light up when brake is pressed.")]
        public VehicleLight stopLights = new VehicleLight();

        /// <summary>
        /// Rear Lights that will light up when headlights are on. Always red.
        /// </summary>
        [Tooltip("Rear Lights that will light up when headlights are on.")]
        public VehicleLight rearLights = new VehicleLight();

        /// <summary>
        /// Rear Lights that will light up when vehicle is in reverse gear(s). Usually white.
        /// </summary>
        [Tooltip("Rear Lights that will light up when vehicle is traveling in reverse. Usually white.")]
        public VehicleLight reverseLights = new VehicleLight();

        /// <summary>
        /// Low beam lights.
        /// </summary>
        [Tooltip("Low beam lights.")]
        public VehicleLight headLights = new VehicleLight();

        /// <summary>
        /// High (full) beam lights.
        /// </summary>
        [Tooltip("High (full) beam lights.")]
        public VehicleLight fullBeams = new VehicleLight();

        /// <summary>
        /// Blinkers on the left side of the vehicle.
        /// </summary>
        [Tooltip("Blinkers on the left side of the vehicle.")]
        public VehicleLight leftBlinkers = new VehicleLight();

        /// <summary>
        /// Blinkers on the right side of the vehicle.
        /// </summary>
        [Tooltip("Blinkers on the right side of the vehicle.")]
        public VehicleLight rightBlinkers = new VehicleLight();

        private VehicleController vc;

        /// <summary>
        /// State in which blinker is at the moment.
        /// </summary>
        public bool BlinkerState
        {
            get
            {
                return (int)(Time.realtimeSinceStartup * 2) % 2 == 0;
            }
        }

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

        /// <summary>
        /// Turns off all lights and emission on all meshes.
        /// </summary>
        public void TurnOffAllLights()
        {
            stopLights.TurnOff();
            headLights.TurnOff();
            rearLights.TurnOff();
            reverseLights.TurnOff();
            fullBeams.TurnOff();
            leftBlinkers.TurnOff();
            rightBlinkers.TurnOff();
        }

        public void Update()
        {
            if(enabled && vc != null)
            {
                // Stop lights
                if (stopLights != null)
                {
                    if (vc.brakes.Active)
                    {
                        stopLights.TurnOn();
                    }
                    else
                    {
                        stopLights.TurnOff();
                    }
                }

                // Reverse lights
                if (reverseLights != null)
                {
                    if(vc.transmission.Gear < 0)
                    {
                        reverseLights.TurnOn();
                    }
                    else
                    {
                        reverseLights.TurnOff();
                    }
                }

                // Lights
                if (rearLights != null && headLights != null)
                {
                    if (vc.input.lowBeamLights)
                    {
                        rearLights.TurnOn();
                        headLights.TurnOn();
                    }
                    else
                    {
                        rearLights.TurnOff();
                        headLights.TurnOff();

                        if (fullBeams != null && fullBeams.On)
                        {
                            fullBeams.TurnOff();
                            vc.input.fullBeamLights = false;
                        }
                    }
                }

                // Full beam lights
                if (fullBeams != null)
                {
                    if (vc.input.fullBeamLights)
                    {
                        fullBeams.TurnOn();

                        if (headLights != null && rearLights != null && !vc.input.lowBeamLights)
                        {
                            headLights.TurnOn();
                            rearLights.TurnOn();
                            vc.input.lowBeamLights = true;
                        }
                    }
                    else
                    {
                        fullBeams.TurnOff();
                    }
                }

                // Left blinker lights (cancels the other blinker)
                if (leftBlinkers != null)
                {
                    if (vc.input.leftBlinker)
                    {
                        if (BlinkerState)
                            leftBlinkers.TurnOn();
                        else
                            leftBlinkers.TurnOff();
                    }
                }

                // Left blinker lights
                if (rightBlinkers != null)
                {
                    if (vc.input.rightBlinker)
                    {
                        if (BlinkerState)
                            rightBlinkers.TurnOn();
                        else
                            rightBlinkers.TurnOff();
                    }
                }

                // Hazards
                if (leftBlinkers != null && rightBlinkers != null)
                {
                    if (vc.input.hazardLights)
                    {
                        if (BlinkerState)
                        {
                            leftBlinkers.TurnOn();
                            rightBlinkers.TurnOn();
                        }
                        else
                        {
                            leftBlinkers.TurnOff();
                            rightBlinkers.TurnOff();
                        }
                    }
                    else
                    {
                        if (!vc.input.leftBlinker)
                            leftBlinkers.TurnOff();

                        if (!vc.input.rightBlinker)
                            rightBlinkers.TurnOff();
                    }
                }
            }

            // Disable all lights if enabled is set to false.
            if(prevEnabled == true && enabled == false)
            {
                TurnOffAllLights();
            }

            prevEnabled = enabled;
        }
    }
}
