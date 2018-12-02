using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    [System.Serializable]
    public class TransmissionWhineComponent : SoundComponent
    {
        /// <summary>
        /// Pitch range that will be added to the base pitch depending on transmission state.
        /// </summary>
        [Tooltip("Pitch range that will be added to the base pitch depending on transmission state.")]
        [Range(0f, 5f)]
        public float pitchRange = 0.2f;

        private float whineVelocity;

        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            if (Clip != null)
            {
                Source = vc.gameObject.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults(Source, true, true, 0, Clip);
                RegisterSources();
            }
        }

        public override void Update()
        {
            if (Clip != null)
            {
                float modifier = vc.engine.RPMPercent * 0.6f + Mathf.Clamp01(vc.Speed / 60f) * 0.4f;
                if (vc.transmission.Gear == 0) modifier = 0;
                Source.pitch = pitch + modifier * pitchRange;
                float targetVolume = volume * (vc.engine.Power / vc.engine.maxPower) * vc.sound.masterVolume;
                if (vc.transmission.GearRatio == 0) targetVolume = 0f;
                Source.volume = Mathf.SmoothDamp(Source.volume, targetVolume, ref whineVelocity, 0.1f);
            }
        }
    }
}
