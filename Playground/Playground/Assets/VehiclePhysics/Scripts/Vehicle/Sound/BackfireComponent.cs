using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Exhaust popping on deceleration / rev limiter.
    /// </summary>
    //[System.Serializable]
    public class BackfireComponent : SoundComponent
    {
        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            if (Clips.Count != 0)
            {
                Source = vc.gameObject.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults(Source, false, true, volume, RandomClip);
                RegisterSources();
            }
        }

        public override void Update()
        {
            if (Source != null && Clip != null)
            {

            }
        }
    }
}
