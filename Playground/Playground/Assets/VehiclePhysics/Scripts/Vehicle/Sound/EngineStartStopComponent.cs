using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Sound of an engine starting / stopping.
    /// First audio clip is for engine starting, and second one is for engine stopping.
    /// </summary>
    [System.Serializable]
    public class EngineStartStopComponent : SoundComponent
    {
        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            // Initilize start/stop source
            if (Clips.Count > 1)
            {
                Source = vc.gameObject.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults(Source, false, false);
                RegisterSources();
                Source.enabled = false;
            }
        }

        public override void Update()
        {
            // Starting and stopping engine sound
            if (Source != null && Clips.Count > 1)
            {
                if ((vc.engine.starting || vc.engine.stopping))
                {
                    if (!Source.enabled) Source.enabled = true;
                    if (vc.engine.starting) Source.clip = Clips[0];
                    if (vc.engine.stopping) Source.clip = Clips[1];

                    float newVolume = volume;

                    if (vc.engine.starting)
                        newVolume = (1f - vc.engine.StartingPercent) * volume;

                    if (vc.engine.stopping)
                        newVolume = (1f - vc.engine.StoppingPercent) * volume;

                    SetVolume(newVolume);

                    if (!Source.isPlaying && Source.enabled)
                        Source.Play();
                }
                else
                {
                    Source.volume = 0;
                    Source.Stop();
                    Source.enabled = false;
                }
            }
        }
    }
}

