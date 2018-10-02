using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Used for adjusting center of mass of any rigidbody object.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [ExecuteInEditMode]
    public class CenterOfMass : MonoBehaviour
    {
        /// <summary>
        /// Center of mass offset in relation to the original COM. Needs to be readjusted when new colliders are added.
        /// </summary>
        [Tooltip("Center of mass offset in relation to the original COM. Needs to be readjusted when new colliders are added.")]
        public Vector3 centerOfMassOffset = Vector3.zero;

        /// <summary>
        /// Enable to show a green spehere at the current center of mass.
        /// </summary>
        [Tooltip("Enable to show a green spehere at the current center of mass.")]
        public bool showCOM = true;

        private Vector3 centerOfMass;
        private Vector3 prevOffset = Vector3.zero;
        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            centerOfMass = rb.centerOfMass;
        }

        void Update()
        {
            if (centerOfMassOffset != prevOffset)
            {
                rb.centerOfMass = centerOfMass + centerOfMassOffset;
            }
            prevOffset = centerOfMassOffset;
        }

        private void OnDrawGizmosSelected()
        {
            if (showCOM && rb != null)
            {
                float radius = 0.1f;
                try
                {
                    radius = GetComponent<MeshFilter>().sharedMesh.bounds.size.z / 30f;
                }
                catch { }

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(rb.transform.TransformPoint(rb.centerOfMass), radius);
            }
        }
    }
}

