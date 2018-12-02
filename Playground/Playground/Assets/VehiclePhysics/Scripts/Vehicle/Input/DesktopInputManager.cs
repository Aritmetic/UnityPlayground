using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Class for handling desktop user input via mouse and keyboard.
    /// Avoid having two input managers active at the sime time (mobile and destop) as the last executed script will override the first one.
    /// </summary>
    [DisallowMultipleComponent]
    public class DesktopInputManager : MonoBehaviour
    {
        /// <summary>
        /// Type of input user input.
        /// Standard - standard keyboard, joystick or gamepad input mapped through the input manager.
        /// Mouse - uses mouse position on screen to control throttle/braking and steering.
        /// MouseSteer - uses LMB / RMB for throttle and braking and mouse for steering.
        /// </summary>
        public enum InputType { Standard, Mouse, MouseSteer }

        [Tooltip("Input type. " +
            "Standard - uses standard input manager for all the inputs. " +
            "Mouse - uses mouse position for steering and throttle. " +
            "MouseSteer - uses mouse position for steering, LMB and RMB for braking / throttle.")]
        public InputType inputType = InputType.Standard;


        public enum VerticalInputType { Standard, ZeroToOne, Composite }

        [Tooltip("Vertical input type." +
            "Standard - uses vertical axis in range of [-1, 1] where -1 is maximum braking and 1 maximum accleration." +
            "ZeroToOne - uses vertical axis in range of [0, 1], 0 being maximum braking and 1 maximum acceleration." + 
            "Composite - uses separate axes, 'Accelerator' and 'Brake' to set the vertical axis value. Still uses a single vartical axis value [-1, 1] " +
            "throughout the system so applying full brakes and gas simultaneously is not possible.")]
        public VerticalInputType verticalInputType = VerticalInputType.Standard;

        /// <summary>
        /// Set to null (none) if you want to use your own vehicle controller. If this is set to other than null current active vehicle according 
        /// to the assigned vehicle changer will be used instead of the assigned vehicle controller.
        /// </summary>
        [Tooltip("Set to null (none) if you want to use your own vehicle controller. If this is set to other than null current active vehicle according " +
            "to the assigned vehicle changer will be used instead of the assigned vehicle controller.")]
        public VehicleChanger vehicleChanger;

        /// <summary>
        /// If you want to use this script with a single vehicle or want to set your own vehicle controller from script set vehicle changer field to null / none.
        /// </summary>
        [Tooltip("If you want to use this script with a single vehicle or want to set your own vehicle controller from script set vehicle changer field to null / none.")]
        public VehicleController vehicleController;

        private float vertical = 0f;
        private float horizontal = 0f;

        
        /// <summary>
        /// Tries to get the button value through input manager, if not falls back to hardcoded default value.
        /// </summary>
        private bool TryGetButton(string buttonName, KeyCode altKey)
        {
            try
            {
                return Input.GetButtonDown(buttonName);
            }
            catch
            {
                Debug.LogWarning(buttonName + " input binding missing, falling back to default. Check Input section in manual for more info.");
                return Input.GetKeyDown(altKey);
            }
        }

        private void Start()
        {
            vehicleController = GetComponent<VehicleController>();
        }


        void Update()
        {
            if (vehicleChanger != null)
            {
                vehicleController = vehicleChanger.ActiveVehicleController;
            }

            try
            {
                vertical = 0f;
                horizontal = 0f;

                if (vehicleController == null) return;

                // Manual shift
                if (TryGetButton("ShiftUp", KeyCode.R))
                    vehicleController.input.ShiftUp = true;

                if (TryGetButton("ShiftDown", KeyCode.F))
                    vehicleController.input.ShiftDown = true;

                if (vehicleController.transmission.transmissionType == Transmission.TransmissionType.Manual)
                {
                    try
                    {
                        if (TryGetButton("FirstGear", KeyCode.Alpha1))
                            vehicleController.transmission.ShiftInto(1);
                        else if (TryGetButton("SecondGear", KeyCode.Alpha2))
                            vehicleController.transmission.ShiftInto(2);
                        else if (TryGetButton("ThirdGear", KeyCode.Alpha3))
                            vehicleController.transmission.ShiftInto(3);
                        else if (TryGetButton("FourthGear", KeyCode.Alpha4))
                            vehicleController.transmission.ShiftInto(4);
                        else if (TryGetButton("FifthGear", KeyCode.Alpha5))
                            vehicleController.transmission.ShiftInto(5);
                        else if (TryGetButton("SixthGear", KeyCode.Alpha6))
                            vehicleController.transmission.ShiftInto(6);
                        else if (TryGetButton("SeventhGear", KeyCode.Alpha7))
                            vehicleController.transmission.ShiftInto(7);
                        else if (TryGetButton("EightGear", KeyCode.Alpha8))
                            vehicleController.transmission.ShiftInto(8);
                        else if (TryGetButton("NinthGear", KeyCode.Alpha9))
                            vehicleController.transmission.ShiftInto(9);
                        else if (TryGetButton("Neutral", KeyCode.Alpha0))
                            vehicleController.transmission.ShiftInto(0);
                        else if (TryGetButton("Reverse", KeyCode.Minus))
                            vehicleController.transmission.ShiftInto(-1);
                    }
                    catch
                    {
                        Debug.LogWarning("Some of the gear changing inputs might not be assigned in the input manager. Direct gear shifting " +
                            "will not work.");
                    }
                }

                // Horizontal axis
                if (inputType == InputType.Standard)
                {
                    horizontal = Input.GetAxis("Horizontal");
                }
                else
                {
                    horizontal = Mathf.Clamp(VehicleController.GetMouseHorizontal(), -1f, 1f);
                }
                vehicleController.input.Horizontal = horizontal;

                // Vertical axis
                if (inputType == InputType.Standard)
                {
                    if(verticalInputType == VerticalInputType.Standard)
                    {
                        vertical = Input.GetAxisRaw("Vertical");
                    }
                    else if (verticalInputType == VerticalInputType.ZeroToOne)
                    {
                        vertical = (Mathf.Clamp01(Input.GetAxisRaw("Vertical")) - 0.5f) * 2f;
                    }
                    else if (verticalInputType == VerticalInputType.Composite)
                    {
                        float accelerator = Mathf.Clamp01(Input.GetAxisRaw("Accelerator"));
                        float brake = Mathf.Clamp01(Input.GetAxisRaw("Brake"));
                        vertical = accelerator - brake;
                    }
                }
                else if (inputType == InputType.Mouse)
                {
                    vertical = Mathf.Clamp(VehicleController.GetMouseVertical(), -1f, 1f);
                }
                else
                {
                    if (Input.GetMouseButton(0))
                        vertical = 1f;
                    else if (Input.GetMouseButton(1))
                        vertical = -1f;
                }
                vehicleController.input.Vertical = vertical;

                // Engine start/stop
                if (TryGetButton("EngineStartStop", KeyCode.E))
                {
                    vehicleController.engine.Toggle();
                }

                // Handbrake
                try
                {
                    vehicleController.input.Handbrake = Input.GetAxis("Handbrake");
                }
                catch
                {
                    Debug.LogWarning("Handbrake axis not set up, falling back to default (Space).");
                    vehicleController.input.Handbrake = Input.GetKey(KeyCode.Space) ? 1f : 0f;
                }

                // Clutch
                if (!vehicleController.transmission.automaticClutch)
                {
                    try
                    {
                        vehicleController.input.Clutch = Input.GetAxis("Clutch");
                    }
                    catch
                    {
                        Debug.LogError("Clutch is set to manual but the required axis 'Clutch' is not set. " +
                            "Please set the axis inside input manager to use this feature.");
                        vehicleController.transmission.automaticClutch = true;
                    }
                }

                // Lights
                if (TryGetButton("LeftBlinker", KeyCode.Z))
                {
                    vehicleController.input.leftBlinker = !vehicleController.input.leftBlinker;
                    if (vehicleController.input.leftBlinker) vehicleController.input.rightBlinker = false;
                }
                if (TryGetButton("RightBlinker", KeyCode.X))
                {
                    vehicleController.input.rightBlinker = !vehicleController.input.rightBlinker;
                    if (vehicleController.input.rightBlinker) vehicleController.input.leftBlinker = false;
                }
                if (TryGetButton("Lights", KeyCode.L)) vehicleController.input.lowBeamLights = !vehicleController.input.lowBeamLights;
                if (TryGetButton("FullBeamLights", KeyCode.K)) vehicleController.input.fullBeamLights = !vehicleController.input.fullBeamLights;
                if (TryGetButton("HazardLights", KeyCode.J))
                {
                    vehicleController.input.hazardLights = !vehicleController.input.hazardLights;
                    vehicleController.input.leftBlinker = false;
                    vehicleController.input.rightBlinker = false;
                }

                // Horn
                vehicleController.input.horn = TryGetButton("Horn", KeyCode.H);

                // Raise trailer flag if trailer attach detach button pressed.
                if (TryGetButton("TrailerAttachDetach", KeyCode.T))
                    vehicleController.input.trailerAttachDetach = true;

                // Manual flip over
                if (vehicleController.flipOver.manual)
                {
                    try
                    {
                        // Set manual flip over flag to true if vehicle is flipped over, otherwise ignore
                        if (Input.GetButtonDown("FlipOver") && vehicleController.flipOver.flippedOver)
                            vehicleController.input.flipOver = true;
                    }
                    catch
                    {
                        Debug.LogError("Flip over is set to manual but 'FlipOver' input binding is not set. Either disable manual flip over or set 'FlipOver' binding.");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("One or more of the required inputs has not been set. Check NWH Vehicle Physics README for more info or add the binding inside Unity input manager.");
                Debug.LogWarning(e);
            }
        }
    }
}

