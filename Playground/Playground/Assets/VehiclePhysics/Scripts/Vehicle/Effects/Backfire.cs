using UnityEngine;
using System.Collections.Generic;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Controls exhaust flames / flashes.
    /// </summary>
    //[System.Serializable]
    public class Backfire
    {
        public float duration = 0.05f;
        public List<MeshRenderer> renderers = new List<MeshRenderer>();

        private float startTime;
        private float progress;
        private VehicleController vc;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

        public void Update()
        {
            if (vc.engine.FuelCutoff) Flash();

            if(renderers != null && renderers.Count > 0)
            {
                progress = Mathf.Clamp01((startTime + duration) - Time.realtimeSinceStartup);

                if (progress > 0)
                {
                    foreach (MeshRenderer renderer in renderers)
                    {
                        renderer.material.SetColor("_TintColor", new Color(1, 1, 1, 1));
                    }
                }
                else
                {
                    foreach (MeshRenderer renderer in renderers)
                    {
                        renderer.material.SetColor("_TintColor", new Color(1, 1, 1, 0));
                    }
                }
            }
        }

        public void Flash()
        {
            startTime = Time.realtimeSinceStartup;
        }
    }
}
