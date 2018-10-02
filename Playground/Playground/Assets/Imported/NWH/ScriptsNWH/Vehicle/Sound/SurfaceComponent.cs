using UnityEngine;
using System.Collections.Generic;
using NWH.WheelController3D;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Sounds produced by tire rolling over the surface.
    /// </summary>
    [System.Serializable]
    public class SurfaceComponent : SoundComponent
    {
        private HysteresisSmoothedValue smoothedVolume;

        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            smoothedVolume = new HysteresisSmoothedValue(0, 0.2f, 0.5f);

            foreach (Wheel wheel in vc.Wheels)
            {
                // Initialize surface audio source
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
                    Sources[i].volume = 0f;
                    Wheel wheel = vc.Wheels[i];
                    GroundDetection.GroundEntity groundEntity = vc.groundDetection.GetCurrentGroundEntity(wheel.WheelController);

                    if (wheel.IsGrounded && groundEntity != null)
                    {
                        if (groundEntity.surfaceSoundComponent.clip != null)
                        {
                            // Change clips based on surface
                            if (Sources[i].clip != groundEntity.surfaceSoundComponent.clip)
                            {
                                // Change surface clip
                                Sources[i].Stop();
                                Sources[i].clip = groundEntity.surfaceSoundComponent.clip;
                                Sources[i].time = Random.Range(0f, groundEntity.surfaceSoundComponent.clip.length);
                                if (Sources[i].enabled) Sources[i].Play();
                            }

                            // If slipSensitiveSurface (tire only makes noise when under load) make sound dependent on side slip, while on gravel and 
                            // similar make sound independent of slip since wheel makes noise at all times (only dependent on speed).
                            float surfaceModifier = 1f;
                            if (groundEntity.slipSensitiveSurfaceSound)
                            {
                                surfaceModifier = (wheel.SmoothSideSlip / vc.sideSlipThreshold) + (wheel.SmoothForwardSlip / vc.forwardSlipThreshold);
                            }

                            // Change surface volume and pitch
                            float newVolume = groundEntity.surfaceSoundComponent.volume * Mathf.Clamp01(vc.Speed / 10f)
                                * surfaceModifier * volume * groundEntity.surfaceSoundComponent.volume;
                            smoothedVolume.Tick(newVolume);

                            SetVolume(smoothedVolume.Value, i);

                            Source.pitch = pitch * 0.5f + Mathf.Clamp01(vc.Speed / 10f) * 1f;
                        }
                    }
                }
            }        
            else
            {
                for (int i = 0; i < vc.Wheels.Count; i++) Sources[i].volume = 0;
            }
        }
    }
}

