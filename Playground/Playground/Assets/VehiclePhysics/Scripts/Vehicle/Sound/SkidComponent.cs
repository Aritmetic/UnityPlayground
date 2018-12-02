using UnityEngine;
using System.Collections.Generic;
using NWH.WheelController3D;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Sound produced by tire skidding over surface.
    /// </summary>
    [System.Serializable]
    public class SkidComponent : SoundComponent
    {
        /// <summary>
        /// Volume of longitudinal (forward) slip / wheel spin sound effect.
        /// </summary>
        [Tooltip("Volume of longitudinal (forward) slip / wheel spin.")]
        [Range(0, 4)]
        public float forwardSkidVolume = 0.8f;

        /// <summary>
        /// Volume of lateral (side) slip sound effect.
        /// </summary>
        [Tooltip("Volume of lateral (side) slip sound effect.")]
        [Range(0, 4)]
        public float sideSkidVolume = 0.9f;


        public HysteresisSmoothedValue smoothedVolume;
        public HysteresisSmoothedValue smoothedPitch;

        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            smoothedVolume = new HysteresisSmoothedValue(0, 0.25f, 0.5f);
            smoothedPitch = new HysteresisSmoothedValue(0, 0.25f, 0.5f);

            foreach (Wheel wheel in vc.Wheels)
            {
                AudioSource a = wheel.ControllerGO.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults( a, true, true);
                Sources.Add(a);
            }

            RegisterSources();
        }

        public override void Update()
        {
            if(vc.groundDetection != null)
            {
                for (int i = 0; i < vc.Wheels.Count; i++)
                {
                    Wheel wheel = vc.Wheels[i];
                    GroundDetection.GroundEntity groundEntity = vc.groundDetection.GetCurrentGroundEntity(wheel.WheelController);

                    if (wheel.IsGrounded && groundEntity != null)
                    {
                        if (groundEntity.skidSoundComponent.clip != null)
                        {
                            if (Sources[i].clip != groundEntity.skidSoundComponent.clip)
                            {
                                // Change skid clip
                                Sources[i].Stop();
                                Sources[i].clip = groundEntity.skidSoundComponent.clip;
                                Sources[i].time = Random.Range(0f, groundEntity.skidSoundComponent.clip.length);
                            }

                            // Update skid sounds
                            if (wheel.HasForwardSlip || wheel.HasSideSlip)
                            {
                                // Calculate skid volume and pitch
                                float forwardSkid = Mathf.Max(0f, (Mathf.Clamp01(Mathf.Pow((Mathf.Abs(wheel.SmoothForwardSlip) - vc.forwardSlipThreshold), 2f)) * forwardSkidVolume));

                                float sideSkid = Mathf.Clamp01(Mathf.Pow((Mathf.Abs(wheel.SmoothSideSlip) - vc.sideSlipThreshold), 2f)) * sideSkidVolume;

                                float skidPitch = Mathf.Clamp(Mathf.Abs(wheel.WheelController.suspensionForce / (vc.vehicleRigidbody.mass * 3f)), groundEntity.skidSoundComponent.pitch * 0.7f, groundEntity.skidSoundComponent.pitch * 1.3f);

                                float skidVolume = Mathf.Clamp01(forwardSkid + sideSkid) * 30f;

                                Sources[i].time = Random.Range(0f, Sources[i].clip.length);
                                float newVolume = Mathf.Clamp01(skidVolume) * volume * groundEntity.skidSoundComponent.volume * vc.sound.masterVolume;
                                smoothedVolume.Tick(newVolume);
                                SetVolume(smoothedVolume.Value, i);

                                smoothedPitch.Tick(skidPitch);
                                Sources[i].pitch = smoothedPitch.Value;

                                if (!Sources[i].isPlaying && Sources[i].isActiveAndEnabled)
                                {
                                    Sources[i].Play();
                                }
                            }
                            else
                            {
                                Sources[i].Stop();
                            }
                        }
                    }
                    else
                    {
                        smoothedVolume.Tick(0);
                    }

                    Sources[i].volume = smoothedVolume.Value;
                }
            }        
        }
    }
}

