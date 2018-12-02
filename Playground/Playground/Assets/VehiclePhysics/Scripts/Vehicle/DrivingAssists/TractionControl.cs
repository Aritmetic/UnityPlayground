using System;
using UnityEngine;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Traction control class.
    /// </summary>
    [System.Serializable]
    public class TractionControl : DrivingAssists.DrivingAid
    {
        /// <summary>
        /// Engine power reduction when wheels start slipping.
        /// </summary>
        [HideInInspector]
        public float powerReduction;

        [HideInInspector]
        public float prevPowerReduction;

        public void Update(VehicleController vc)
        {
            active = false;
            prevPowerReduction = vc.engine.TcsPowerReduction;
            vc.engine.TcsPowerReduction = 0f;

            if (vc.Speed > 0.5f && !vc.brakes.Active)
            {
                foreach (Wheel wheel in vc.Wheels)
                {
                    float maxTorque = wheel.WheelController.MaxPutDownForce * wheel.Radius * (1f + (0.5f - intensity * 0.5f));

                    if (wheel.MotorTorque > maxTorque)
                    {
                        wheel.MotorTorque = maxTorque;
                        active = true;
                    }
                    else if (wheel.MotorTorque < -maxTorque)
                    {
                        wheel.MotorTorque = -maxTorque;
                        active = true;
                    }
                }
            }
        }
    }
}
