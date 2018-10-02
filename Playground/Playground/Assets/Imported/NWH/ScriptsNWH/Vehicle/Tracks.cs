using UnityEngine;
using System.Collections.Generic;
using NWH.WheelController3D;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Class for handling tracked vehicles.
    /// If enabled all the wheels should be the same size and placed on either left or right side of the vehicle. Center wheels are not supported in this mode.
    /// </summary>
    [System.Serializable]
    public class Tracks
    {
        /// <summary>
        /// True if vehicle uses tracks instead of wheels.
        /// </summary>
        [Tooltip("True if vehicle uses tracks instead of wheels.")]
        public bool trackedVehicle = false;

        /// <summary>
        /// If larger than zero virtual wheel size inside simulation will be increased at initialization time which results in better and more
        /// track-like behavior and prevents tank from getting stuck on small objects that would normally be able to wedge themselves between individual wheels.
        /// </summary>
        [Tooltip("If larger than zero virtual wheel size inside simulation will be increased at initialization time which results in better and more " +
            "track-like behavior and prevents tank from getting stuck on small objects that would normally be able to wedge themselves between individual wheels.")]
        [Range(1f, 2f)]
        public float wheelEnlargementCoefficient = 2f;

        /// <summary>
        /// Speed at which the track texture will be moved. Needs to be adjusted to match wheel rotation speed.
        /// </summary>
        [Tooltip("Speed at which the track texture will be moved. Needs to be adjusted to match wheel rotation speed.")]
        public float textureOffsetSpeedCoefficient = 1f;

        /// <summary>
        /// Renderers for left track. Will be used to offset the track texture to imitate moving tracks.
        /// </summary>
        [Tooltip("Renderers for left track. Will be used to offset the track texture to imitate moving tracks.")]
        public Renderer leftTrackRenderer;

        /// <summary>
        /// Renderers for right track. Will be used to offset the track texture to imitate moving tracks.
        /// </summary>
        [Tooltip("Renderers for right track. Will be used to offset the track texture to imitate moving tracks.")]
        public Renderer rightTrackRenderer;

        /// <summary>
        /// Left side drive sprocket and other rotating wheels that are not wheel controllers but need to be rotated with track.
        /// </summary>
        [Tooltip("Left side drive sprocket and other rotating wheels that are not wheel controllers but need to be rotated with track.")]
        public List<GameObject> leftSprockets = new List<GameObject>();

        /// <summary>
        /// Right side drive sprocket and other rotating wheels that are not wheel controllers but need to be rotated with track.
        /// </summary>
        [Tooltip("Right side drive sprocket and other rotating wheels that are not wheel controllers but need to be rotated with track.")]
        public List<GameObject> rightSprockets = new List<GameObject>();

        private VehicleController vc;
        private float maxLeftRpm;
        private float maxRightRpm;
        private float averageRadius;
        private float leftDistance;
        private float rightDistance;
        private bool initialized;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;

            float radiusSum = 0;
            foreach (Wheel wheel in vc.Wheels)
            {
                wheel.WheelController.trackedVehicle = true;
                radiusSum += wheel.Radius;
            }
            averageRadius = radiusSum / vc.Wheels.Count;
        }


        public void Update()
        {
            // Offset Texture
            maxLeftRpm = maxRightRpm = 0f;

            if (vc == null) return;

            foreach (Wheel wheel in vc.Wheels)
            {
                if(wheel.WheelController.VehicleSide == WheelController.Side.Left)
                {
                    if(Mathf.Abs(wheel.RPM) > maxLeftRpm)
                    {
                        maxLeftRpm = wheel.RPM;
                    }
                }
                else if(wheel.WheelController.VehicleSide == WheelController.Side.Right)
                {
                    if(Mathf.Abs(wheel.RPM) > maxRightRpm)
                    {
                        maxRightRpm = wheel.RPM;
                    }
                }
            }

            leftDistance = -averageRadius * maxLeftRpm * 0.10472f * textureOffsetSpeedCoefficient * Time.fixedDeltaTime;
            rightDistance = -averageRadius * maxRightRpm * 0.10472f * textureOffsetSpeedCoefficient * Time.fixedDeltaTime;

            if(leftTrackRenderer != null)
            {
                leftTrackRenderer.material.SetTextureOffset("_MainTex", new Vector2(leftTrackRenderer.material.mainTextureOffset.x + leftDistance, leftTrackRenderer.material.mainTextureOffset.y));
            }

            if(rightTrackRenderer != null)
            {
                rightTrackRenderer.material.SetTextureOffset("_MainTex", new Vector2(rightTrackRenderer.material.mainTextureOffset.x + rightDistance, rightTrackRenderer.material.mainTextureOffset.y));
            }

            if(leftSprockets != null && leftSprockets.Count > 0)
            {
                foreach(GameObject sprocket in leftSprockets)
                {
                    // 0.016667 * 360 = 6.00012
                    float rotationAngle = maxLeftRpm * 6.00012f * Time.fixedDeltaTime;
                    sprocket.transform.Rotate(new Vector3(rotationAngle, 0f, 0f));
                }
            }

            if(rightSprockets != null && rightSprockets.Count > 0)
            {
                foreach (GameObject sprocket in rightSprockets)
                {
                    float rotationAngle = maxRightRpm * 6.00012f * Time.fixedDeltaTime;
                    sprocket.transform.Rotate(new Vector3(rotationAngle, 0f, 0f));
                }
            }
        }

    }
}

