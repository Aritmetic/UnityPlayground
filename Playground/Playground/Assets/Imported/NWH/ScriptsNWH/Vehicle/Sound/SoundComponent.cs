using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Base abstract class from which all vehicle sound components inherit.
    /// </summary>
    [System.Serializable]
    public abstract class SoundComponent
    {
        /// <summary>
        /// Base volume of the sound component.
        /// </summary>
        [Tooltip("Base volume of the sound component.")]
        [Range(0f, 1f)]
        public float volume = 0.1f;

        /// <summary>
        /// Base pitch of the sound component.
        /// </summary>
        [Tooltip("Base pitch of the sound component.")]
        [Range(0f, 2f)]
        public float pitch = 1f;

        /// <summary>
        /// List of audio clips this component can use. Some components can use multiple clips in which case they will be chosen at random, and some components can use only one 
        /// in which case only the first clip will be selected. Check manual for more details.
        /// </summary>
        [Tooltip("List of audio clips this component can use. Some components can use multiple clips in which case they will be chosen at random, and some components can use only one " +
            "in which case only the first clip will be selected. Check manual for more details.")]
        public List<AudioClip> clips = new List<AudioClip>();

        protected List<AudioSource> sources = new List<AudioSource>();
        protected VehicleController vc;
        protected AudioMixerGroup audioMixerGroup;

        /// <summary>
        /// Adds outputs of sources to the mixer.
        /// </summary>
        public void RegisterSources()
        {
            foreach(AudioSource source in sources)
            {
                source.outputAudioMixerGroup = audioMixerGroup;
            }
        }

        /// <summary>
        /// Gets or sets the first clip in the clips list.
        /// </summary>
        public AudioClip Clip
        {
            get
            {
                if(clips.Count > 0)
                {
                    return clips[0];
                }
                return null;
            }
            set
            {
                if(clips.Count > 0)
                {
                    clips[0] = value;
                }
                clips.Add(value);
            }
        }

        /// <summary>
        /// Gets or sets the whole clip list.
        /// </summary>
        public List<AudioClip> Clips
        {
            get
            {
                return clips;
            }
            set
            {
                clips = value;
            }
        }

        /// <summary>
        /// Gets or sets the first audio source in the sources list.
        /// </summary>
        public AudioSource Source
        {
            get
            {
                if(sources.Count > 0)
                {
                    return sources[0];
                }
                return null;
            }
            set
            {
                if(sources.Count > 0)
                {
                    sources[0] = value;
                }
                sources.Add(value);
            }
        }

        /// <summary>
        /// Gets or sets the whole sources list.
        /// </summary>
        public List<AudioSource> Sources
        {
            get
            {
                return sources;
            }
            set
            {
                sources = value;
            }
        }


        /// <summary>
        /// Gets a random clip from clips list.
        /// </summary>
        public AudioClip RandomClip
        {
            get
            {
                return clips[Random.Range(0, clips.Count)];
            }
        }


        /// <summary>
        /// Sets volume for the [id]th source in sources list. Use instead of directly changing source volume as this takes master volume into account.
        /// </summary>
        public void SetVolume(float volume, int id)
        {
            sources[id].volume = volume * vc.sound.masterVolume;
        }

        /// <summary>
        /// Sets volume for the first source in sources list. Use instead of directly changing source volume as this takes master volume into account.
        /// </summary>
        public void SetVolume(float volume)
        {
            Source.volume = volume * vc.sound.masterVolume;
        }

        public void Enable()
        {
            foreach(AudioSource source in sources)
            {
                if (!source.enabled) source.enabled = true;
            }
        }

        public void Disable()
        {
            foreach (AudioSource source in sources)
            {
                if (source.enabled) source.enabled = false;
            }
        }

        public abstract void Initialize(VehicleController vc, AudioMixerGroup amg);
        public abstract void Update();
    }
}
