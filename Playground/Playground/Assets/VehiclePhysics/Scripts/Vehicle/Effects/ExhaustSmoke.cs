using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Controls particle emitters that represent exhausts.
    /// </summary>
    [System.Serializable]
    public class ExhaustSmoke
    {
        /// <summary>
        /// Particles will emit from the exhaust if set to true.
        /// </summary>
        [Tooltip("Particles will emit from the exhaust if set to true.")]
        public bool emit = false;

        /// <summary>
        /// Exhaust smoke intensity when vehicle is idle.
        /// </summary>
        [Tooltip("Exhaust smoke intensity when vehicle is idle.")]
        [Range(0, 30)]
        public float baseIntensity = 12f;

        /// <summary>
        /// Intensity range which will be added to the base intensity depending on engine state.
        /// </summary>
        [Tooltip("Intensity range which will be added to the base intensity depending on engine state.")]
        [Range(0, 30)]
        public float intensityRange = 20f;

        /// <summary>
        /// Amount of soot that will be present in the final color when engine is under heavy load.
        /// </summary>
        [Tooltip("Amount of soot that will be present in the final color when engine is under heavy load.")]
        [Range(0, 1)]
        public float soot = 0.4f;

        /// <summary>
        /// Size of each particle at start.
        /// </summary>
        [Tooltip("Size of each particle at start.")]
        [Range(0, 1)]
        public float startSize = 0.3f;

        /// <summary>
        /// How far behind the vehicle will exhaust particles extend?
        /// </summary>
        [Tooltip("How far behind the vehicle will exhaust particles extend?")]
        [Range(0, 10)]
        public float lifeDistance = 2.5f;

        /// <summary>
        /// Color of exhaust at idle.
        /// </summary>
        [Tooltip("Color of exhaust at idle.")]
        public Color vaporColor = new Color(0.7f, 0.7f, 0.7f, 0.35f);

        /// <summary>
        ///  Color of exhaust at heavy load.
        /// </summary>
        [Tooltip("Color of exhaust at heavy load.")]
        public Color sootColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        /// <summary>
        /// Color of exhaust when engine is running clean.
        /// </summary>
        [Tooltip("Color of exhaust when engine is running clean.")]
        private Color cleanColor = new Color(0f, 0f, 0f, 0f);

        /// <summary>
        /// List of particle systems representing exhausts.
        /// </summary>
        [Tooltip("List of particle systems representing exhausts.")]
        public List<ParticleSystem> particleSystems = new List<ParticleSystem>();
        private VehicleController vc;

        private Color idleColor;
        private float sootIntensity;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

        public void Update()
        {
            if (vc.Active && vc.engine.IsRunning)
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    var emission = ps.emission;
                    if(!emission.enabled) emission.enabled = true;
                    var main = ps.main;
                    main.startSpeed = 0.3f + vc.engine.RPMPercent * 1f + vc.Load;
                    main.startSize = startSize;

                    // Always emit for particle to only reach 1 meter
                    float lifetime = vc.Speed == 0 ? 0 : lifeDistance / vc.Speed;
                    main.startLifetime = lifeDistance == 0 ? 0 
                        : Mathf.Lerp(lifeDistance, lifetime, Mathf.Clamp01(vc.Speed / lifeDistance));

                    idleColor = cleanColor;
                    if (vc.transmission.Gear == 0)
                        idleColor = Color.Lerp(vaporColor, cleanColor, Mathf.Clamp01((vc.engine.RPM - vc.engine.minRPM) / 400f));

                    sootIntensity = Mathf.Clamp01(vc.Load) * soot * 5f;

                    if(vc.damage.enabled)
                    {
                        sootIntensity += vc.damage.DamagePercent;
                    }

                    main.startColor = Color.Lerp(idleColor, sootColor, sootIntensity);

                    float speedBias = lifeDistance == 0 ? 0 : Mathf.Clamp01(vc.Speed / lifeDistance);
                    float rate = baseIntensity + intensityRange * vc.engine.RPMPercent;
                    emission.rateOverTime = rate * (1f - speedBias);
                    emission.rateOverDistance = rate * speedBias;
                }
            }
            else
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    var emission = ps.emission;
                    emission.enabled = false;
                }
            }
        }
    }
}

