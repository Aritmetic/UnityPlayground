using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Loose approximation on downforce acting on a vehicle. Downforce in this case is only 
    /// dependent on speed, but its amount and speed at which it is achieved can be adjusted.
    /// Can be adjusted at runtime.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public class Downforce : MonoBehaviour
    {
        /// <summary>
        /// Single point at which downforce will be applied.
        /// </summary>
        [System.Serializable]
        public class DownforcePoint
        {
            /// <summary>
            /// Position relative to the vehicle at which downforce will be applied. Marked by red arrow gizmo.
            /// </summary>
            [Tooltip("Position relative to the vehicle at which downforce will be applied. Marked by red arrow gizmo.")]
            public Vector3 position;

            /// <summary>
            /// Maximim force in [N] that can be applied as a result of downforce. 
            /// Putting in a too large value will hammer the vehicle into the ground since springs get overloaded.
            /// </summary>
            [Tooltip("Maximim force in [N] that can be applied as a result of downforce.")]
            public float maxForce;
        }

        [Tooltip("List of points at which downforce will be applied.")]
        public List<DownforcePoint> downforcePoints = new List<DownforcePoint>();

        /// <summary>
        /// Speed in m/s at which maximum downforce will be applied. Amount of downforce will grow exponentially up to this value.
        /// </summary>
        public float maxDownforceSpeed = 50f;

        private VehicleController vc;

        private void Start()
        {
            vc = GetComponent<VehicleController>();
        }

        void Update()
        {
            float speedPercent = vc.Speed / maxDownforceSpeed;
            float forceCoeff = 1f - (1f - Mathf.Pow(speedPercent, 2f));

            foreach (DownforcePoint dp in downforcePoints)
            {
                vc.vehicleRigidbody.AddForceAtPosition(forceCoeff * dp.maxForce * -transform.up, transform.TransformPoint(dp.position));
            }
        }

        private void OnDrawGizmosSelected()
        {
            foreach (DownforcePoint dp in downforcePoints)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.TransformPoint(dp.position), 0.1f);
            }
        }
    }
}

