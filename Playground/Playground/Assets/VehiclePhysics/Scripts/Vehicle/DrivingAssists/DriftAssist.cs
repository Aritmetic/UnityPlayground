using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Setting other than 0 will add forces that act as to prevent vehicle from spinning out when drifting.
    /// Effect does not work at low speeds to allow for doughnuts.
    /// </summary>
    [System.Serializable]
    public class DriftAssist : DrivingAssists.DrivingAid
    {
        public void Update(VehicleController vc)
        {
            // Drift assist
            if(intensity > 0)
            {
                Vector3 normVel = vc.vehicleRigidbody.velocity.normalized;
                Vector3 vehicleDir = vc.transform.forward;
                float driftAngle = VehicleController.AngleSigned(normVel, vehicleDir, vc.transform.up);
                driftAngle = Mathf.Sign(driftAngle) * Mathf.Clamp(Mathf.Abs(Mathf.Clamp(driftAngle, -90f, 90f)), 0f, Mathf.Infinity);

                if (vc.axles.Count > 0)
                {
                    Axle a = vc.axles[vc.axles.Count - 1];
                    Vector3 center = (a.leftWheel.ControllerTransform.position + a.rightWheel.ControllerTransform.position) / 2f;
                    float forceMag = driftAngle * Mathf.Lerp(0f, vc.vehicleRigidbody.mass, vc.Speed / 15f) * intensity;
                    Vector3 force = vc.transform.right * forceMag;
                    vc.vehicleRigidbody.AddForceAtPosition(force, center);
                }
            }
        }
    }
}
