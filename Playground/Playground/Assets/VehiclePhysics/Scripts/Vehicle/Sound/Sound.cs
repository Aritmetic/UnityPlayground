using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Main class that manages all the sound aspects of the vehicle.
    /// </summary>
    [System.Serializable]
    public class Sound
    {
        /// <summary>
        /// Spatial blend of all audio sources. Can not be changed at runtime.
        /// </summary>
        [Tooltip("Spatial blend of all audio sources. Can not be changed at runtime.")]
        [Range(0, 1)]
        public float spatialBlend = 1f;

        /// <summary>
        /// Master volume of a vehicle. To adjust volume of all vehicles or their components check audio mixer.
        /// </summary>
        [Tooltip("Master volume of a vehicle. To adjust volume of all vehicles or their components check audio mixer.")]
        [Range(0, 2)]
        public float masterVolume = 1f;

        [Header("Engine")]

        /// <summary>
        /// Sound of engine idling.
        /// </summary>
        [Tooltip("Sound of engine idling.")]
        public EngineIdleSoundComponent engineIdleComponent = new EngineIdleSoundComponent();

        /// <summary>
        /// Engine start / stop component. First clip is for starting and second one is for stopping.
        /// </summary>
        [Tooltip("Engine start / stop component. First clip is for starting and second one is for stopping.")]
        public EngineStartStopComponent engineStartStopComponent = new EngineStartStopComponent();

        /// <summary>
        /// Forced induction whistle component. Can be used for air intake noise or supercharger if spool up time is set to 0 under engine settings.
        /// </summary>
        [Tooltip("Forced induction whistle component. Can be used for air intake noise or supercharger if spool up time is set to 0 under engine settings.")]
        public TurboWhistleComponent turboWhistleComponent = new TurboWhistleComponent();

        /// <summary>
        /// Sound of turbo's wastegate. Supports multiple clips.
        /// </summary>
        [Tooltip("Sound of turbo's wastegate. Supports multiple clips.")]
        public TurboFlutterComponent turboFlutterComponent = new TurboFlutterComponent();

        /// <summary>
        /// Exhaust popping sound on deceleration / rev limiter.
        /// </summary>
        [Tooltip("Sound of turbo's wastegate. Supports multiple clips.")]
        public BackfireComponent exhaustPopComponent = new BackfireComponent();

        [Header("Transmission")]

        /// <summary>
        /// Transmission whine from straight cut gears or just a noisy gearbox.
        /// </summary>
        [Tooltip("Transmission whine from straight cut gears or just a noisy gearbox.")]
        public TransmissionWhineComponent transmissionWhineComponent = new TransmissionWhineComponent();

        /// <summary>
        /// Sound from changing gears. Supports multiple clips.
        /// </summary>
        [Tooltip("Sound from changing gears. Supports multiple clips.")]
        public GearChangeComponent gearChangeComponent = new GearChangeComponent();

        [Header("Suspension")]

        /// <summary>
        /// Sound from wheels hitting ground and/or obstracles. Supports multiple clips.
        /// </summary>
        [Tooltip("Sound from wheels hitting ground and/or obstracles. Supports multiple clips.")]
        public SuspensionComponent suspensionComponent = new SuspensionComponent();

        [Header("Surface Noise")]

        /// <summary>
        /// Sound produced by wheel rolling over a surface. Tire hum.
        /// </summary>
        [Tooltip("Sound produced by wheel rolling over a surface. Tire hum.")]
        public SurfaceComponent surfaceComponent = new SurfaceComponent();

        /// <summary>
        /// Sound produced by wheel skidding over a surface. Tire squeal.
        /// </summary>
        [Tooltip("Sound produced by wheel skidding over a surface. Tire squeal.")]
        public SkidComponent skidComponent = new SkidComponent();

        [Header("Crash")]

        /// <summary>
        /// Sound of vehicle hitting other objects. Supports multiple clips.
        /// </summary>
        [Tooltip("Sound of vehicle hitting other objects. Supports multiple clips.")]
        public CrashComponent crashComponent = new CrashComponent();

        [Header("Other")]

        /// <summary>
        /// Sound of air brakes releasing air. Supports multiple clips.
        /// </summary>
        [Tooltip("Sound of air brakes releasing air. Supports multiple clips.")]
        public AirBrakeComponent airBrakeComponent = new AirBrakeComponent();

        /// <summary>
        /// Tick-tock sound of a working blinker. First clip is played when blinker is turning on and second clip is played when blinker is turning off.
        /// </summary>
        [Tooltip("Tick-tock sound of a working blinker. First clip is played when blinker is turning on and second clip is played when blinker is turning off.")]
        public BlinkerComponent blinkerComponent = new BlinkerComponent();

        [Tooltip("Horn sound.")]
        public HornComponent hornComponent = new HornComponent();

        [Header("Interior Parameters")]

        /// <summary>
        /// Set to true if listener inside vehicle. Mixer must be set up.
        /// </summary>
        [Tooltip("Set to true if listener is inside the vehicle. Mixer must be set up.")]
        public bool insideVehicle = false;
        private bool wasInsideVehickle = false;

        /// <summary>
        /// Sound attenuation inside vehicle.
        /// </summary>
        [Tooltip("Sound attenuation inside vehicle.")]
        public float interiorAttenuation = -7f;

        public float lowPassFrequency = 6000f;

        [Range(1f, 10f)]
        public float lowPassQ = 1f;


        [HideInInspector]
        public AudioMixerGroup masterGroup;

        [HideInInspector]
        public AudioMixerGroup engineMixerGroup;

        [HideInInspector]
        public AudioMixerGroup transmissionMixerGroup;

        [HideInInspector]
        public AudioMixerGroup surfaceNoiseMixerGroup;

        [HideInInspector]
        public AudioMixerGroup turboMixerGroup;

        [HideInInspector]
        public AudioMixerGroup suspensionMixerGroup;

        [HideInInspector]
        public AudioMixerGroup crashMixerGroup;

        [HideInInspector]
        public AudioMixerGroup otherMixerGroup;

        private float originalAttenuation;

        private List<SoundComponent> components = new List<SoundComponent>();
        private AudioMixer audioMixer;

        private VehicleController vc;

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;

            audioMixer = Resources.Load("VehicleAudioMixer") as AudioMixer;
            masterGroup = audioMixer.FindMatchingGroups("Master")[0];
            engineMixerGroup = audioMixer.FindMatchingGroups("Engine")[0];
            transmissionMixerGroup = audioMixer.FindMatchingGroups("Transmission")[0];
            surfaceNoiseMixerGroup = audioMixer.FindMatchingGroups("SurfaceNoise")[0];
            turboMixerGroup = audioMixer.FindMatchingGroups("Turbo")[0];
            suspensionMixerGroup = audioMixer.FindMatchingGroups("Suspension")[0];
            crashMixerGroup = audioMixer.FindMatchingGroups("Crash")[0];
            otherMixerGroup = audioMixer.FindMatchingGroups("Other")[0];

            // Remember initial states
            audioMixer.GetFloat("attenuation", out originalAttenuation);

            /*
            * IMPORTANT
            * When adding a new sound component also add it to the list below so it can be enabled / disabled when vehicle is activated or suspended.
            */
            components = new List<SoundComponent>
            {
                engineIdleComponent,
                engineStartStopComponent,
                skidComponent,
                surfaceComponent,
                turboFlutterComponent,
                turboWhistleComponent,
                transmissionWhineComponent,
                gearChangeComponent,
                airBrakeComponent,
                blinkerComponent,
                hornComponent, 
                exhaustPopComponent
            };

            // Do not use following components if vehicle is trailer.
            if (!vc.trailer.isTrailer)
            {
                engineStartStopComponent.Initialize(vc, engineMixerGroup);
                engineIdleComponent.Initialize(vc, engineMixerGroup);
                exhaustPopComponent.Initialize(vc, engineMixerGroup);
                turboWhistleComponent.Initialize(vc, turboMixerGroup);
                turboFlutterComponent.Initialize(vc, turboMixerGroup);
                transmissionWhineComponent.Initialize(vc, transmissionMixerGroup);
                gearChangeComponent.Initialize(vc, transmissionMixerGroup);
                airBrakeComponent.Initialize(vc, otherMixerGroup);
                blinkerComponent.Initialize(vc, otherMixerGroup);
                hornComponent.Initialize(vc, otherMixerGroup);
            }

            skidComponent.Initialize(vc, surfaceNoiseMixerGroup);
            surfaceComponent.Initialize(vc, surfaceNoiseMixerGroup);
            crashComponent.Initialize(vc, crashMixerGroup);
            suspensionComponent.Initialize(vc, suspensionMixerGroup);
        }


        public void Update()
        {
            // Adjust sound if inside vehicle.
            if (!wasInsideVehickle && insideVehicle)
            {
                audioMixer.SetFloat("attenuation", interiorAttenuation);
                audioMixer.SetFloat("lowPassFrequency", lowPassFrequency);
                audioMixer.SetFloat("lowPassQ", lowPassQ);
            }
            else if(wasInsideVehickle && !insideVehicle)
            {
                audioMixer.SetFloat("attenuation", originalAttenuation);
                audioMixer.SetFloat("lowPassFrequency", 22000f);
                audioMixer.SetFloat("lowPassQ", 1f);
            }
            wasInsideVehickle = insideVehicle;

            // Update individual components.
            if (!vc.trailer.isTrailer)
            {
                engineIdleComponent.Update();
                engineStartStopComponent.Update();
                turboWhistleComponent.Update();
                turboFlutterComponent.Update();
                gearChangeComponent.Update();
                transmissionWhineComponent.Update();
                airBrakeComponent.Update();
                blinkerComponent.Update();
                hornComponent.Update();
                exhaustPopComponent.Update();
            }

            skidComponent.Update();
            surfaceComponent.Update();
            suspensionComponent.Update();
        }

        /// <summary>
        /// Initializes audio source to it's starting values.
        /// </summary>
        /// <param name="audioSource">AudioSource in question.</param>
        /// <param name="play">Play on awake?</param>
        /// <param name="loop">Should clip be looped?</param>
        /// <param name="volume">Volume of the audio source.</param>
        /// <param name="clip">Clip that will be set at the start.</param>
        public void SetAudioSourceDefaults(AudioSource audioSource, bool play = false, bool loop = false, float volume = 0f, AudioClip clip = null)
        {
            if (audioSource != null)
            {
                audioSource.spatialBlend = spatialBlend;
                audioSource.playOnAwake = play;
                audioSource.loop = loop;
                audioSource.volume = volume * vc.sound.masterVolume;
                audioSource.clip = clip;
                audioSource.priority = 200;

                if (play)
                    audioSource.Play();
                else
                    audioSource.Stop();
            }
            else
            {
                Debug.LogWarning("AudioSource is null. Defaults cannot be set.");
            }
        }

        /// <summary>
        /// Enable sound.
        /// </summary>
        public void Enable()
        {
            foreach (SoundComponent sc in components)
            {
                if (sc != null) sc.Enable();
            }
        }

        /// <summary>
        /// Disable all sound components.
        /// </summary>
        public void Disable()
        {
            foreach (SoundComponent sc in components)
            {
                if (sc != null && sc != engineStartStopComponent)
                {
                    sc.Disable();
                }
            }
        }

        /// <summary>
        /// Sets defaults to all the basic sound components when script is first added or reset is called.
        /// </summary>
        public void SetDefaults()
        {
            // Set defaults to each sound component as they all inherit from the same base class and are set to same default values by default.

            engineIdleComponent.volume = 0.3f;
            engineIdleComponent.pitch = 0.9f;

            engineStartStopComponent.volume = 0.4f;

            transmissionWhineComponent.volume = 0.05f;
            transmissionWhineComponent.pitch = 0.1f;

            gearChangeComponent.volume = 0.1f;

            suspensionComponent.volume = 0.1f;

            surfaceComponent.volume = 0.25f;

            skidComponent.volume = 0.45f;

            crashComponent.volume = 0.32f;

            turboWhistleComponent.volume = 0.04f;
            turboWhistleComponent.pitch = 0.8f;

            turboFlutterComponent.volume = 0.04f;

            try
            {
                if (this != null)
                {
                    if (engineStartStopComponent.Clips.Count < 2)
                    {
                        engineStartStopComponent.Clips.Add(Resources.Load("Defaults/EngineStart") as AudioClip);
                        engineStartStopComponent.Clips.Add(Resources.Load("Defaults/EngineStop") as AudioClip);
                    }

                    if (engineIdleComponent.Clip == null)
                    {
                        engineIdleComponent.Clip = Resources.Load("Defaults/EngineIdle") as AudioClip;
                    }

                    if (gearChangeComponent.Clips.Count == 0)
                        gearChangeComponent.Clips.Add(Resources.Load("Defaults/GearShift") as AudioClip);

                    if (turboWhistleComponent.Clip == null)
                        turboWhistleComponent.Clip = Resources.Load("Defaults/TurboWhistle") as AudioClip;

                    if (turboFlutterComponent.Clip == null)
                        turboFlutterComponent.Clip = Resources.Load("Defaults/TurboFlutter") as AudioClip;

                    if (suspensionComponent.Clip == null)
                        suspensionComponent.Clip = Resources.Load("Defaults/SuspensionThump") as AudioClip;

                    if (crashComponent.Clips.Count == 0)
                        crashComponent.Clips.Add(Resources.Load("Defaults/Crash") as AudioClip);

                    if (blinkerComponent.Clips.Count == 0)
                    {
                        blinkerComponent.clips.Add(Resources.Load("Defaults/BlinkerOn") as AudioClip);
                        blinkerComponent.clips.Add(Resources.Load("Defaults/BlinkerOff") as AudioClip);
                    }

                    if (hornComponent.Clip == null)
                        hornComponent.Clip = Resources.Load("Defaults/Horn") as AudioClip;
                }
            }
            catch
            {
                Debug.LogWarning("One or more of the default sound resources could not be found. Default sounds will not be assigned.");
            }
        }
    }
}

