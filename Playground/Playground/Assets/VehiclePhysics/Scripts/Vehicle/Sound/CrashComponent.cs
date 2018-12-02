using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Sound of vehicle crashing into an object.
    /// Supports multiple audio clips of which one will be chosen at random each time this effect is played.
    /// </summary>
    [System.Serializable]
    public class CrashComponent : SoundComponent
    {
        public override void Initialize(VehicleController vc, AudioMixerGroup amg)
        {
            this.vc = vc;
            this.audioMixerGroup = amg;

            if (Clip != null)
            {
                Source = vc.gameObject.AddComponent<AudioSource>();
                vc.sound.SetAudioSourceDefaults(Source, false, false);
                RegisterSources();
            }
        }

        public override void Update() { }

        public void Play(VehicleController vc)
        {
            if (Clips.Count > 0)
            {
                if (vc.CollisionInfo != null)
                {
                    ContactPoint[] contactPoints = vc.CollisionInfo.contacts;
                    bool rimHit = true;
                    for (int i = 0; i < contactPoints.Length; i++)
                    {
                        if (contactPoints[i].thisCollider.name != "RimCollider")
                        {
                            rimHit = false;
                            break;
                        }
                    }

                    if (!rimHit)
                    {
                        float collisionMagnitude = Mathf.Abs(vc.Acceleration.magnitude);
                        if (vc.GetCollisionState() == VehicleController.VehicleCollisionState.Stay)
                        {
                            Source.clip = RandomClip;
                            SetVolume(Mathf.Clamp01(collisionMagnitude / 2000f) * volume);
                            Source.pitch = Random.Range(0.6f, 1.4f);
                            Source.Play();
                        }
                    }
                }
            }
        }
    }
}

