using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Main class for handling visual effects such as skidmarks, lights and exhausts.
    /// </summary>
    [System.Serializable]
    public class Effects
    {
        public Skidmarks skidmarks = new Skidmarks();
        public Lights lights = new Lights();
        public ExhaustSmoke exhausts = new ExhaustSmoke();
        public Backfire exhaustFlash = new Backfire();

        private List<SurfaceParticles> particleList = new List<SurfaceParticles>();
        private VehicleController vc;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;

            if (vc.groundDetection != null)
            {
                foreach (Wheel wheel in vc.Wheels)
                {
                    SurfaceParticles particle = new SurfaceParticles();
                    particle.Initialize(vc, wheel);
                    particleList.Add(particle);
                }
            }

            skidmarks.Initialize(vc);
            exhausts.Initialize(vc);
            exhaustFlash.Initialize(vc);
            lights.Initialize(vc);
        }

        public void Update()
        {
            if (vc != null && vc.groundDetection != null)
            {
                foreach (SurfaceParticles particle in particleList)
                {
                    particle.Update();
                }
            }

            skidmarks.Update();
            exhausts.Update();
            exhaustFlash.Update();
            lights.Update();
        }

    }
}

