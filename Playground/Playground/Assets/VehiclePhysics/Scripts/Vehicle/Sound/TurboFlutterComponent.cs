using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Sound of wastegate releasing air on turbocharged vehicles.
    /// </summary>
    [System.Serializable]
    public class TurboFlutterComponent : SoundComponent
    {
        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            if (Clip != null)
            {
                Source = vc.gameObject.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults(Source, false, false, volume, Clip);
                RegisterSources();
            }
        }

        public override void Update()
        {
            if (Clip != null)
            {
                if (vc.engine.forcedInduction.flutterSoundFlag)
                {
                    vc.engine.forcedInduction.flutterSoundFlag = false;
                    Source.pitch = pitch + pitch * Random.Range(-0.3f, 0.3f);
                    float newVolume = Mathf.Clamp01(volume * Mathf.Pow(vc.engine.forcedInduction.SpoolPercent, 4f));
                    if(newVolume > 0.2f * volume)
                    {
                        SetVolume(newVolume);
                        Source.Play();
                    }
                }
            }
        }
    }
}