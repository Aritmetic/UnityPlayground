using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Sound of turbocharger or supercharger.
    /// </summary>
    [System.Serializable]
    public class TurboWhistleComponent : SoundComponent
    {
        /// <summary>
        /// Pitch range that will be added to the base pitch depending on turbos's RPM.
        /// </summary>
        [Range(0, 5)]
        public float pitchRange = 1.4f;

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
            if (Clip != null && vc.engine.IsRunning && vc.engine.forcedInduction.useForcedInduction)
            {
                SetVolume(Mathf.Clamp01(volume * Mathf.Pow(vc.engine.forcedInduction.SpoolPercent, 1.4f)) * vc.sound.masterVolume);
                Source.pitch = pitch + pitchRange * vc.engine.forcedInduction.SpoolPercent;
            }
            else
            {
                if(Source != null)
                    Source.volume = 0;
            }
        }
    }
}

