using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Sound of an engine idling.
    /// </summary>
    [System.Serializable]
    public class EngineIdleSoundComponent : SoundComponent
    {
        /// <summary>
        /// Volume added to the base engine volume depending on engine state.
        /// </summary>
        [Tooltip("Volume added to the base engine volume depending on engine state.")]
        [Range(0, 1)]
        public float volumeRange = 0.5f;

        /// <summary>
        /// Pitch added to the base engine pitch depending on engine RPM.
        /// </summary>
        [Tooltip("Pitch added to the base engine pitch depending on engine RPM.")]
        [Range(0, 4)]
        public float pitchRange = 1.5f;

        /// <summary>
        /// Smoothing of engine sound.
        /// </summary>
        [Tooltip("Smoothing of engine sound.")]
        [Range(0, 1)]
        public float smoothing = 0.1f;

        /// <summary>
        /// Distortion that will be added to the engine sound through mixer when under heavy load / high RPM.
        /// </summary>
        [Tooltip("Distortion that will be added to the engine sound through mixer when under heavy load / high RPM.")]
        [Range(0, 1)]
        public float maxDistortion = 0.4f;

        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            // Initialize engine sound
            if (Clip != null)
            {
                Source = vc.gameObject.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults(Source, true, true, 0f, Clip);
                RegisterSources();
                Source.Stop();
                SetVolume(0);
            }
        }

        public override void Update()
        {
            // Engine sound
            if (Source != null && Clip != null)
            {
                if (vc.engine.IsRunning || vc.engine.starting || vc.engine.stopping)
                {
                    if (!Source.isPlaying && Source.enabled) Source.Play();

                    float rpmModifier = Mathf.Clamp01((vc.engine.RPM - vc.engine.minRPM) / vc.engine.maxRPM);
                    float newPitch = pitch + rpmModifier * pitchRange;
                    Source.pitch = Mathf.Lerp(Source.pitch, newPitch, 1f - smoothing);

                    float volumeModifier = 0;
                    if (vc.transmission.Gear == 0)
                    {
                        volumeModifier = rpmModifier;
                    }
                    else
                    {
                        volumeModifier = rpmModifier * 0.65f + Mathf.Abs(vc.input.Vertical) * 0.35f;
                    }

                    float newVolume = (volume + Mathf.Clamp01(volumeModifier) * volumeRange);

                    // Set distortion
                    audioMixerGroup.audioMixer.SetFloat("engineDistortion", Mathf.Lerp(0f, maxDistortion, volumeModifier));

                    if (vc.engine.starting)
                        newVolume = vc.engine.StartingPercent * volume;

                    if (vc.engine.stopping)
                        newVolume = (1f - vc.engine.StoppingPercent) * volume;

                    // Add random offsets if vehicle damaged
                    if (vc.damage.enabled && vc.damage.performanceDegradation)
                    {
                        float damageRandomRange = 0.2f * vc.damage.DamagePercent;
                        float damageOffset = Random.Range(-damageRandomRange, damageRandomRange) * volumeRange;
                        newVolume *= (1f + damageOffset);
                        Source.pitch += damageOffset * Source.pitch;
                    }

                    SetVolume(newVolume);
                }
                else
                {
                    if (Source.isPlaying) Source.Stop();
                    Source.volume = 0;
                    Source.pitch = 0;
                }
            }
        }
    }
}

