using System;
using UnityEngine;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Traction control class.
    /// </summary>
    [System.Serializable]
    public class ABS : DrivingAssists.DrivingAid
    {
        public void Update(VehicleController vc)
        {
            active = false;
            if (vc.brakes.Active && vc.engine.RpmOverflow <= 0f && !vc.engine.FuelCutoff && vc.input.Handbrake == 0)
            {
                foreach (Wheel wheel in vc.Wheels)
                {
                    float maxTorque = wheel.WheelController.MaxPutDownForce * wheel.Radius * (1f + (0.5f - intensity * 0.5f));

                    if (wheel.BrakeTorque > maxTorque)
                    {
                        wheel.BrakeTorque = maxTorque;
                        active = true;
                    }
                    else if (wheel.BrakeTorque < -maxTorque)
                    {
                        wheel.BrakeTorque = -maxTorque;
                        active = true;
                    }
                }
            }
        }
    }
}
