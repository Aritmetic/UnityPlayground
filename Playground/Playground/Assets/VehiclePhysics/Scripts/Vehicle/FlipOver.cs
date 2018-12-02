using UnityEngine;
using System.Collections;

namespace NWH.VehiclePhysics
{
    [System.Serializable]
    public class FlipOver
    {
        /// <summary>
        /// Should the vehicle be rotated back when it flips over?
        /// </summary>
        [Tooltip("Should the vehicle be rotated back when it flips over?")]
        public bool enabled = true;

        /// <summary>
        /// If enabled a prompt will be shown after the timeout, asking player to press the FlipOver button.
        /// </summary>
        [Tooltip("If enabled a prompt will be shown after the timeout, asking player to press the FlipOver button.")]
        public bool manual = false;

        /// <summary>
        /// Time after detecting flip over after which vehicle will be flipped back.
        /// </summary>
        [Tooltip("Time after detecting flip over after which vehicle will be flipped back or the manual button can be used.")]
        public float timeout = 3f;

        /// <summary>
        /// Minimum angle that the vehicle needs to be at for it to be detected as flipped over.
        /// </summary>
        [Tooltip("Minimum angle that the vehicle needs to be at for it to be detected as flipped over.")]
        public float allowedAngle = 70f;

        /// <summary>
        /// Flip over detection will be disabled if velocity is above this value [m/s].
        /// </summary>
        [Tooltip("Flip over detection will be disabled if velocity is above this value [m/s].")]
        public float maxDetectionSpeed = 1f;

        /// <summary>
        /// Rotation speed of the vehicle while being flipped back.
        /// </summary>
        [Tooltip("Rotation speed of the vehicle while being flipped back.")]
        public float rotationSpeed = 80f;

        /// <summary>
        /// Is the vehicle flipped over?
        /// </summary>
        [HideInInspector]
        public bool flippedOver = false;

        private bool wasFlippedOver = false;
        private float timeSinceFlip = 0f;
        private float timeAfterRecovery = 0f;
        private VehicleController vc;
        private float direction = 0;
        private bool manualFlipoverInProgress = false;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

        public void Update()
        {
            DetectFlipOver();

            // Return if flip over disabled.
            if (!enabled) return;

            if (manual)
            {
                if (vc.input.flipOver)
                {
                    manualFlipoverInProgress = true;
                    vc.input.flipOver = false;
                }
            }

            if ((flippedOver && !manual) || (flippedOver && manual && manualFlipoverInProgress))
            {
                if (direction == 0)
                {
                    direction = Mathf.Sign(Vector3.SignedAngle(vc.transform.up, -Physics.gravity.normalized, vc.transform.forward) - 180f);
                }

                vc.vehicleRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                Quaternion yRotation = Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, vc.transform.InverseTransformDirection(vc.transform.forward));
                vc.vehicleRigidbody.MoveRotation(vc.transform.rotation * yRotation);
            }
            else if(wasFlippedOver && !flippedOver)
            {
                vc.vehicleRigidbody.constraints = RigidbodyConstraints.None;
                manualFlipoverInProgress = false;
            }

            wasFlippedOver = flippedOver;
        }

        void DetectFlipOver()
        {
            int wheelsOnGround = 0;
            foreach(Wheel wheel in vc.Wheels)
            {
                if(wheel.IsGrounded)
                {
                    wheelsOnGround++;
                }
            }

            if(vc.Speed < maxDetectionSpeed && VehicleAngle() > allowedAngle && wheelsOnGround <= vc.Wheels.Count / 2f)
            {
                timeSinceFlip += Time.fixedDeltaTime;

                if(timeSinceFlip > timeout)
                {
                    flippedOver = true;
                }
            }
            else
            {
                timeAfterRecovery += Time.fixedDeltaTime;

                if(timeAfterRecovery > 1f || VehicleAngle() < 45f)
                {
                    flippedOver = false;
                    timeSinceFlip = 0f;
                    timeAfterRecovery = 0f;
                    direction = 0;
                }
            }
        }

        float VehicleAngle()
        {
            return Vector3.Angle(vc.transform.up, -Physics.gravity.normalized);
        }
    }
}

