using System;
using UnityEngine;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Stability assist class. Unlike the Arcade/Anti Slip option stability will try to prevent any slip.
    /// </summary>
    [System.Serializable]
    public class Stability : DrivingAssists.DrivingAid
    {
        public void Update(VehicleController vc)
        {
            foreach (Axle axle in vc.axles)
            {
                foreach (Wheel wheel in vc.Wheels) if (wheel.WheelController.isGrounded)
                    {
                        float forceMag = wheel.WheelController.sideFriction.force * Mathf.Clamp01(Mathf.Abs(wheel.WheelController.sideFriction.slip)) * intensity * (vc.Speed / 6f);
                        Vector3 force = wheel.WheelController.wheelHit.sidewaysDir * -forceMag;
                        vc.vehicleRigidbody.AddForceAtPosition(force, wheel.WheelController.wheelHit.groundPoint);
                    }
            }
        }
    }
}
