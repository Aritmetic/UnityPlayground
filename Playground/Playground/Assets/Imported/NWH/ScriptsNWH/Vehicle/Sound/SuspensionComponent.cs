using UnityEngine;
using System.Collections.Generic;
using NWH.WheelController3D;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Sound of wheel hitting the surface or obstracle.
    /// </summary>
    [System.Serializable]
    public class SuspensionComponent : SoundComponent
    {
        private List<bool> prevHasHits = new List<bool>();

        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            foreach (Wheel wheel in vc.Wheels)
            {
                // Initialize surface audio source
                AudioSource a = wheel.ControllerGO.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults(a, false, false, volume);
                Sources.Add(a);

                bool hasHit = true;
                prevHasHits.Add(hasHit);
            }

            RegisterSources();
        }

        public override void Update()
        {
            if (Clip != null && Sources != null && Sources.Count == vc.Wheels.Count)
            {
                for (int i = 0; i < vc.Wheels.Count; i++)
                {
                    WheelController wc = vc.Wheels[i].WheelController;
                    if (!Sources[i].isPlaying)
                    {
                        if ((wc.isGrounded && prevHasHits[i] == false) || (wc.forwardFriction.speed > 0.8f && Mathf.Abs(wc.wheelHit.angleForward * Mathf.Rad2Deg) > 20f))
                        {
                            Sources[i].pitch = Random.Range(pitch - 0.15f, pitch + 0.15f);
                            Sources[i].clip = RandomClip;
                            SetVolume(volume * Mathf.Clamp01(wc.pointVelocity.magnitude / 5f), i);
                            Sources[i].Play();
                        }
                    }

                    prevHasHits[i] = wc.isGrounded;
                }
            }
        }
    }
}