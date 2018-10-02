using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Hiss produced by air brakes releasing air.
    /// Accepts multiple clips of which one will be chosen at random each time this effect is played.
    /// </summary>
    [System.Serializable]
    public class AirBrakeComponent : SoundComponent
    {
        private bool prevActive = false;

        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            if(Clip != null)
            {
                Source = vc.gameObject.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults(Source, false, false, volume);
                RegisterSources();
            }
        }

        public override void Update()
        {
            if(Clip != null)
            {
                if (prevActive && !vc.brakes.Active && vc.Speed < 1f && !Source.isPlaying)
                {
                    Source.clip = RandomClip;
                    SetVolume(Mathf.Clamp01(vc.brakes.airBrakePressure / 10f) * volume);
                    Source.Play();
                    vc.brakes.airBrakePressure = 0;
                }
                prevActive = vc.brakes.Active;
            }
        }
    }
}
