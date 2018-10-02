using UnityEngine;
using System.Collections;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Camera for on or in-vehicle use with option of head movement according to the G-forces.
    /// </summary>
    public class CameraOnboard : MonoBehaviour
    {
        /// <summary>
        /// Vehicle Controller that this script is targeting. Can be left empty if head movement is not being used.
        /// </summary>
        [Tooltip("Vehicle Controller that this script is targeting. Can be left empty if head movement is not being used.")]
        public VehicleController vehicleController;

        [Header("Head Movement")]

        /// <summary>
        /// Smoothing of the head movement.
        /// </summary>
        [Tooltip("Smoothing of the head movoement.")]
        [Range(0f, 1f)]
        public float positionSmoothing = 0.3f;

        /// <summary>
        /// How much will the head move around for the given g-force.
        /// </summary>
        [Tooltip("How much will the head move around for the given g-force.")]
        [Range(0f, 1f)]
        public float positionIntensity = 0.125f;

        /// <summary>
        /// Maximum head movement from the initial position.
        /// </summary>
        [Tooltip("Maximum head movement from the initial position.")]
        [Range(0f, 1f)]
        public float maxPositionOffsetMagnitude = 0.2f;

        private Vector3 positionOffset;
        private Vector3 accelerationChangeVelocity;
        private Vector3 offsetChangeVelocity;
        private Vector3 prevAcceleration;
        private Vector3 localAcceleration;
        private Vector3 newPositionOffset;

        Vector3 initialPosition;

        private void Awake()
        {
            initialPosition = vehicleController.transform.InverseTransformPoint(transform.position);
        }

        void FixedUpdate()
        {
            transform.position = vehicleController.transform.TransformPoint(initialPosition);

            localAcceleration = Vector3.zero;
            if(vehicleController != null)
            {
                localAcceleration = vehicleController.transform.TransformDirection(vehicleController.Acceleration);
            }

            newPositionOffset = (Vector3.SmoothDamp(prevAcceleration, localAcceleration, ref accelerationChangeVelocity, positionSmoothing) / 100f) 
                * positionIntensity;
            positionOffset = Vector3.SmoothDamp(positionOffset, newPositionOffset, ref offsetChangeVelocity, positionSmoothing);
            positionOffset.y *= 0.3f;
            positionOffset.x *= 0.7f;
            positionOffset = Vector3.ClampMagnitude(positionOffset, maxPositionOffsetMagnitude);
            transform.position -= vehicleController.transform.TransformDirection(positionOffset);

            if(vehicleController != null)
            {
                prevAcceleration = vehicleController.Acceleration;
            }
        }
    }
}

