using UnityEngine;
using System.Collections.Generic;

namespace NWH.VehiclePhysics
{
    [System.Serializable]
    public class Skidmarks
    {
        /// <summary>
        /// Should skidmarks be generated?
        /// </summary>
        [Tooltip("Should skidmarks be generated?")]
        public bool enabled = true;

        /// <summary>
        /// Higher value will give darker skidmarks for the same skid value.
        /// </summary>
        [Tooltip("Higher value will give darker skidmarks for the same skid value.")]
        [Range(0, 1)]
        public float skidmarkStrength = 0.5f;

        /// <summary>
        /// Max skidmark texture alpha.
        /// </summary>
        [Tooltip("Max skidmark texture alpha.")]
        [Range(0, 1)]
        public float maxSkidmarkAlpha = 0.6f;

        /// <summary>
        /// If enabled skidmarks will stay on the ground until distance from the vehicle becomes greater than persistentSkidmarkDistance.
        /// If disabled skidmarks will stay on the ground until maxMarksPerSection is reached and then will start getting deleted from the end.
        /// </summary>
        [Tooltip("If enabled skidmarks will stay on the ground until distance from the vehicle becomes greater than persistentSkidmarkDistance. " +
            "If disabled skidmarks will stay on the ground until maxMarksPerSection is reached and then will start getting deleted from the oldest skidmark.")]
        public bool persistentSkidmarks = false;

        /// <summary>
        /// Persistent skidmarks get deleted when distance from the parent vehicle is higher than this.
        /// </summary>
        [Tooltip("Persistent skidmarks get deleted when distance from the parent vehicle is higher than this.")]
        public float persistentSkidmarkDistance = 100f;

        /// <summary>
        /// Number of skidmarks that will be drawn per one section, before mesh is saved and new one is generated.
        /// </summary>
        [Tooltip("Number of skidmarks that will be drawn per one section, before mesh is saved and new one is generated.")]
        public int maxMarksPerSection = 120;

        /// <summary>
        /// Distance from the last skidmark section needed to generate a new one.
        /// </summary>
        [Tooltip("Distance from the last skidmark section needed to generate a new one.")]
        public float minDistance = 0.11f;

        /// <summary>
        /// Optional. Thread albedo texture that will be used on skidmark. If left empty default texture from skidmark material will be left unchanged.
        /// </summary>
        [Tooltip("Optional. Thread albedo texture that will be used on skidmark. If left empty default texture from skidmark material will be left unchanged.")]
        public Texture2D threadAlbedo = null;

        /// <summary>
        /// Optional. Thread bump texture that will be used on skidmark. If left empty default texture from skidmark material will be left unchanged.
        /// </summary>
        [Tooltip("Optional. Thread bump texture that will be used on skidmark. If left empty default texture from skidmark material will be left unchanged.")]
        public Texture2D threadBump = null;

        /// <summary>
        /// Optional. Thread normal texture that will be used on skidmark. If left empty default texture from skidmark material will be left unchanged.
        /// </summary>
        [Tooltip("Optional. Thread bump texture that will be used on skidmark. If left empty default texture from skidmark material will be left unchanged.")]
        public Texture2D threadNormal = null;

        private List<SkidmarkGenerator> skidmarkList = new List<SkidmarkGenerator>();

        private VehicleController vc;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;

            if (vc.groundDetection != null)
            {
                foreach (Wheel wheel in vc.Wheels)
                {
                    SkidmarkGenerator skidmark = new SkidmarkGenerator();
                    skidmark.maxMarks = maxMarksPerSection;
                    skidmark.Initialize(vc, wheel);
                    skidmarkList.Add(skidmark);
                }
            }

            float minPersistentDistance = maxMarksPerSection * minDistance * 1.5f;
            if(persistentSkidmarkDistance < minPersistentDistance)
            {
                persistentSkidmarkDistance = minPersistentDistance;
            }
        }

        public void Update()
        {
            if (enabled && vc != null && vc.groundDetection != null)
            {
                foreach (SkidmarkGenerator skidmark in skidmarkList)
                {
                    skidmark.Update();
                }
            }
        }
    }
}

