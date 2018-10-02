using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Digital and analog gauge controller. Can be used to create on-screen gauges or dash gauges inside vehicles.
    /// All of the gauges and lights are optional, e.g. only one gauge or light can be used.
    /// If you want to use this script with only one vehicle do not assign vehicle changer as the script will then use the current vehicle from vehicle changer.
    /// Check prefabs folder for gauge and light prefabs.
    /// </summary>
    public class DashGUIController : MonoBehaviour
    {
        public VehicleController vehicleController;
        public VehicleChanger vehicleChanger;

        public AnalogGauge analogRpmGauge;
        public AnalogGauge analogSpeedGauge;
        public DigitalGauge digitalSpeedGauge;
        public DigitalGauge digitalGearGauge;

        public DashLight leftBlinker;
        public DashLight rightBlinker;
        public DashLight lowBeam;
        public DashLight highBeam;
        public DashLight TCS;
        public DashLight ABS;
        public DashLight checkEngine;

        void Update()
        {
            if (vehicleChanger != null)
            {
                vehicleController = vehicleChanger.ActiveVehicleController;
            }

            if (vehicleController != null)
            {
                if (analogRpmGauge != null) analogRpmGauge.Value = vehicleController.engine.RPM;
                if (analogSpeedGauge != null) analogSpeedGauge.Value = vehicleController.Speed * 3.6f;
                if (digitalSpeedGauge != null) digitalSpeedGauge.numericalValue = vehicleController.Speed * 3.6f;
                if (digitalGearGauge != null) digitalGearGauge.stringValue = vehicleController.transmission.Gear.ToString();

                if (leftBlinker != null) leftBlinker.Active = vehicleController.effects.lights.leftBlinkers.On;
                if (rightBlinker != null) rightBlinker.Active = vehicleController.effects.lights.rightBlinkers.On;
                if (lowBeam != null) lowBeam.Active = vehicleController.effects.lights.headLights.On;
                if (highBeam != null) highBeam.Active = vehicleController.effects.lights.fullBeams.On;
                if (TCS != null) TCS.Active = vehicleController.drivingAssists.tcs.active;
                if (ABS != null) ABS.Active = vehicleController.drivingAssists.abs.active;
                if (checkEngine != null)
                {
                    if (vehicleController.damage.DamagePercent > 0.5f)
                        checkEngine.Active = true;
                    else
                        checkEngine.Active = false;
                }

                if (vehicleController.engine.starting)
                {
                    if (leftBlinker != null) leftBlinker.Active = true;
                    if (rightBlinker != null) rightBlinker.Active = true;
                    if (lowBeam != null) lowBeam.Active = true;
                    if (highBeam != null) highBeam.Active = true;
                    if (TCS != null) TCS.Active = true;
                    if (ABS != null) ABS.Active = true;
                    if (checkEngine != null) checkEngine.Active = true;
                }
            }
            else
            {
                if (analogRpmGauge != null) analogRpmGauge.Value = 0;
                if (analogSpeedGauge != null) analogSpeedGauge.Value = 0;
                if (digitalSpeedGauge != null) digitalSpeedGauge.numericalValue = 0;
                if (digitalGearGauge != null) digitalGearGauge.stringValue = "";
                if (leftBlinker != null) leftBlinker.Active = false;
                if (rightBlinker != null) rightBlinker.Active = false;
                if (lowBeam != null) lowBeam.Active = false;
                if (highBeam != null) highBeam.Active = false;
                if (TCS != null) TCS.Active = false;
                if (ABS != null) ABS.Active = false;
                if (checkEngine != null) checkEngine.Active = false;
            }
        }

    }
}
