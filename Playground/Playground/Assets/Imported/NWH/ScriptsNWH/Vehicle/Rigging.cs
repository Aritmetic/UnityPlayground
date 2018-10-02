using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWH.VehiclePhysics
{
    [System.Serializable]
    public class Rigging
    {
        /// <summary>
        /// Should rigging be used?
        /// </summary>
        [Tooltip("Should rigging be used?")]
        public bool enabled = false;

        /// <summary>
        /// If enabled wheel camber will be set as if the wheels were on a solid axle.
        /// </summary>
        [Tooltip("If enabled wheel camber will be set as if the wheels were on a solid axle.")]
        public bool solidAxle = false;

        /// <summary>
        /// List of handles controlling the axle bones. Each item is a single axle handle.
        /// </summary>
        [Tooltip("List of handles controlling the axle bones. Each item is a single axle.")]
        [SerializeField]
        public List<Transform> axleBones = new List<Transform>();

        /// <summary>
        /// List of handles controlling the wheel bones. Each item is a single wheel bone handle.
        /// </summary>
        [Tooltip("List of handles controlling the wheel bones. Each item is a single wheel bone handle.")]
        [SerializeField]
        public List<Transform> wheelBones = new List<Transform>();

        public void Update(VehicleController vc)
        {
            if (!enabled)
                return;

            if(solidAxle)
            {
                if(axleBones.Count == vc.axles.Count && axleBones != null)
                {
                    for (int i = 0; i < axleBones.Count; i++)
                    {
                        // Adjust axle position
                        Axle axle = vc.axles[i];

                        Vector3 position = (axle.leftWheel.WheelController.springTravelPoint + axle.rightWheel.WheelController.springTravelPoint) / 2f;
                        axleBones[i].position = position;
                        axleBones[i].LookAt(axle.leftWheel.WheelController.springTravelPoint, vc.transform.up);
                        axleBones[i].Rotate(0, 90, 0);

                        // Adjust camber
                        float camberAngle = VehicleController.AngleSigned(vc.transform.right, axleBones[i].right, vc.transform.forward);
                        axle.leftWheel.WheelController.SetCamber(camberAngle);
                        axle.rightWheel.WheelController.SetCamber(-camberAngle);
                    }
                }
                else
                {
                    Debug.LogError("Number of axle bones must be the same as the number of axles.");
                }
            }
            else
            {
                if(wheelBones.Count == vc.Wheels.Count && wheelBones != null)
                {
                    for (int i = 0; i < wheelBones.Count; i++)
                    {
                        wheelBones[i].position = vc.Wheels[i].WheelController.springTravelPoint;
                    }
                }
                else
                {
                    Debug.LogError("Number of wheel bones must be the same as the number of wheels.");
                }
            }
        }
    }
}

