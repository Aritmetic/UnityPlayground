using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Everything related to a vehicle's engine and it's systems.
    /// </summary>
    [System.Serializable]
    public class Engine
    {
        /// <summary>
        /// Determines the state of the engine.
        /// </summary>
        [Tooltip("Determines if engine will already be running when scene is started.")]
        public bool runOnStartup = false;

        /// <summary>
        /// Will the engine auto-start when vehicle is enabled?
        /// </summary>
        [Tooltip("Will the engine auto-start when vehicle is enabled? (i.e. entering vehicle)")]
        public bool runOnEnable = true;

        /// <summary>
        /// Will the engine auto-stop when vehicle is disabled?
        /// </summary>
        [Tooltip("Will the engine auto-stop when vehicle is disabled? (i.e. exiting vehicle)")]
        public bool stopOnDisable = true;

        /// <summary>
        /// True when engine is starting.
        /// </summary>
        [HideInInspector]
        public bool starting = false;

        /// <summary>
        /// True when engine is stopping.
        /// </summary>
        [HideInInspector]
        public bool stopping = false;

        /// <summary>
        /// Minimum RPM that engine will run at. RPM can not go below this value.
        /// </summary>
        [Tooltip("Minimum RPM that the enigne will run at. RPM can not go below this value.")]
        [ShowInTelemetry, ShowInSettings(400, 1200, 100)]
        public float minRPM = 600;

        /// <summary>
        /// Maximum RPM that engine will run at. 
        /// </summary>
        [Tooltip("Maximum RPM that the engine will run at. RPM can not go above this value.")]
        [ShowInTelemetry, ShowInSettings(2500, 10000, 500)]
        public float maxRPM = 5000;

        /// <summary>
        /// Power at the peak of the power curve.
        /// </summary>
        [Tooltip("Power at the peak of the power curve.")]
        [ShowInTelemetry, ShowInSettings(50, 500, 25)]
        public float maxPower = 150;

        /// <summary>
        /// Maximum RPM change per second when engine is running without load or when wheels are slipping. 
        /// Can be used to immitate the flywheel.
        /// </summary>
        [Tooltip("Maximum RPM change per second when engine is running without load or when wheels are slipping." +
            "Can be used to immitate the flywheel.")]
        public float maxRpmChange = 10000;

        /// <summary>
        /// Power delivery smoothing so that the vehicle does not go from 0 to full power instantly. 
        /// Number represents time needed to reach the input [s].
        /// Smoothing only works on throttle, off throttle there is no smoothing.
        /// </summary>
        [Tooltip("Power delivery smoothing so that the vehicle does not go from 0 to full power instantly. " +
            "Number represents time needed to reach the input [s]." +
            "Smoothing is only on throttle, off throttle there is no smoothing.")]
        [Range(0f, 1f)]
        public float throttleSmoothing = 0.2f;

        /// <summary>
        /// Curve showing how power (Y axis) depends on RPM of the engine (shown on X axis as percentage).
        /// </summary>
        [Tooltip("Curve showing how power (Y axis) depends on RPM of the engine (shown on X axis as percentage).")]
        public AnimationCurve powerCurve = new AnimationCurve(new Keyframe[3] {
                new Keyframe(0f, 0f),
                new Keyframe(0.75f, 1f),
                new Keyframe(1f, 0.92f)
            });

        private bool isRunning = true;
        private bool wasRunning = false;

        [ShowInTelemetry]
        private float rpm;
        private float prevRpm;
        private float rpmOverflow;

        [ShowInTelemetry]
        private float power;

        [ShowInTelemetry]
        private float throttle = 0f;
        private float prevThrottle = 0f;
        private float throttleVelocity = 0f;

        private float startDuration = 1f;
        private float stopDuration = 1f;
        private float startedTime = -1;
        private float stoppedTime = -1;

        private float fuelCutoffStart;
        private float fuelCutoffDuration = 0.01f;

        [Tooltip("Only relevant if the engine has forced induction - Forced Induction Type is set to other than none. " +
            "Apart from sound effects it also adds power to the engine.")]
        public ForcedInduction forcedInduction = new ForcedInduction();

		private VehicleController vc;

        /// <summary>
        /// Returns true if engine is running. To start or stop the engine call Start() or Stop() respectively.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
        }

        /// <summary>
        /// Value from 0 to 1 determining how far into the starting process the engine is.
        /// </summary>
        public float StartingPercent
        {
            get
            {
                if (startedTime >= 0)
                    return Mathf.Clamp01((Time.realtimeSinceStartup - startedTime) / startDuration);
                else
                    return 1;
            }
        }

        /// <summary>
        /// Value from 0 to 1 determining how far into the stopping process the engine is.
        /// </summary>
        public float StoppingPercent
        {
            get
            {
                if (stoppedTime >= 0)
                    return Mathf.Clamp01((Time.realtimeSinceStartup - stoppedTime) / stopDuration);
                else
                    return 1;
            }
        }

        /// <summary>
        /// State of fuel cutoff.
        /// </summary>
        public bool FuelCutoff
        {
            get
            {
                if (Time.realtimeSinceStartup < fuelCutoffStart + fuelCutoffDuration)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Current RPM of the engine.
        /// </summary>
        public float RPM
        {
            get
            {
                float r = 0;
                if (isRunning && !starting && !stopping)
                {
                    r = Mathf.Clamp(rpm, minRPM, maxRPM);
                }
                else
                {
                    if(starting)
                    {
                        r = StartingPercent * minRPM;
                    }
                    else if(stopping)
                    {
                        r = (1f - StoppingPercent) * minRPM;
                    }
                }
                return r;
            }
        }

        /// <summary>
        /// 0 if engine is operating between minRPM and maxRPM. Larger then 0 when engine RPM is over maxRPM and less than 0 when
        /// engine is under minRPM. This is single-frame overflow only, before RPM gets clamped.
        /// </summary>
        public float RpmOverflow
        {
            get { return rpmOverflow; }
        }

        /// <summary>
        /// Value in betwwen 0 and 1 where 0 indicates that engine is at minRPM and 1 indicates that engine is at maxRPM.
        /// </summary>
        public float RPMPercent
        {
            get
            {
                return Mathf.Clamp01((RPM - minRPM) / (maxRPM - minRPM));
            }
        }

        /// <summary>
        /// Current power of the engine derived from the engine RPM and the powerCurve.
        /// </summary>
        [ShowInTelemetry]
        public float Power
        {
            get
            {
                return power;
            }
        }

        /// <summary>
        /// Power in horsepower instead of kW.
        /// </summary>
        public float PowerInHP
        {
            get
            {
                return Power * 1.341f;
            }
        }

        /// <summary>
        /// Power reduction from TCS kicking in.
        /// </summary>
        public float TcsPowerReduction
        {
            get
            {
                return vc.drivingAssists.tcs.powerReduction;
            }
            set
            {
                vc.drivingAssists.tcs.powerReduction = Mathf.Clamp01(value);
            }
        }


        /// <summary>
        /// Power reduction from all the vehicle's systems.
        /// </summary>
        public float TotalPowerReduction
        {
            get
            {
                return Mathf.Clamp01(TcsPowerReduction + vc.trailer.NoTrailerPowerReduction);
            }
        }

        /// <summary>
        /// Torque engine is putting out.
        /// </summary>
        [ShowInTelemetry]
        public float Torque
        {
            get
            {
                if (RPM > 0)
                {
                    return (9548f * Power) / RPM;
                }
                return 0;
            }
        }

        /// <summary>
        /// Starts the engine. Can be interrupted by calling Stop(). Will not work if there is no fuel or vehicle is damaged over damage threshold.
        /// </summary>
        public void Start()
        {
            if(!vc.fuel.HasFuel || (vc.damage.enabled && vc.damage.DamagePercent == 1f))
            {
                vc.sound.engineStartStopComponent.Source.Play();
                isRunning = false;
            }
            else
            {
                wasRunning = isRunning ? true : false;
                isRunning = true;

                if (!wasRunning)
                {
                    startedTime = Time.realtimeSinceStartup;
                    if (vc.sound != null && vc.sound.engineStartStopComponent.Clip != null)
                        startDuration = vc.sound.engineStartStopComponent.Clip.length;
                    else
                        startDuration = 0;
                }
            }
        }


        /// <summary>
        /// Stops the engine. Can be interrupted by calling Start().
        /// </summary>
        public void Stop()
        {
            wasRunning = isRunning ? true : false;
            isRunning = false;
            stopping = true;
            stoppedTime = Time.realtimeSinceStartup;
        }


        /// <summary>
        /// Toggles the engine state using Start() and Stop().
        /// </summary>
        public void Toggle()
        {
            if (isRunning)
                Stop();
            else
                Start();
        }


        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
            forcedInduction.Initialize(vc);

            starting = false;
            stopping = false;
            wasRunning = false;
            isRunning = false;

            if (vc.sound.engineStartStopComponent.Clips.Count > 0)
                startDuration = vc.sound.engineStartStopComponent.Clips[0].length * 0.9f;

            if(!isRunning && runOnStartup)
            {
                Start();
            }
        }


        public void Update()
        {
            if (isRunning == true && wasRunning == false)
            {
                startedTime = Time.realtimeSinceStartup;
                wasRunning = true;
                starting = true;
                stopping = false;
            }

            if (starting == true && Time.realtimeSinceStartup > startedTime + startDuration)
            {
                starting = false;
            }

            if (stopping == true && Time.realtimeSinceStartup > stoppedTime + stopDuration)
            {
                stopping = false;
            }

            if (isRunning)
            {
                float allowedRpmChange = maxRpmChange * Time.fixedDeltaTime * powerCurve.Evaluate(rpm/maxRPM);

                forcedInduction.Update();

                prevRpm = rpm;
                rpmOverflow = 0;

                vc.transmission.UpdateClutch();

                // Limit rev Range
                if (rpm > maxRPM)
                {
                    rpmOverflow = rpm - maxRPM;
                    rpm = maxRPM;
                    StartFuelCutoff();
                }
                else if (rpm < minRPM)
                {
                    rpmOverflow = rpm - minRPM;
                    rpm = minRPM;
                }

                // Get RPM from wheels if not in neutral
                if (vc.transmission.Gear != 0 && !vc.transmission.Shifting)
                {
                    rpm = vc.transmission.ReverseRPM;
                    rpm += vc.transmission.AddedClutchRPM;

                    if (rpm < minRPM) rpm = minRPM;

                    if (rpm > (prevRpm + allowedRpmChange))
                        rpm = prevRpm + allowedRpmChange;
                    else if (rpm < (prevRpm - allowedRpmChange))
                        rpm = prevRpm - allowedRpmChange;
                }
                // Engine is not connected
                else
                {
                    float userInput = Mathf.Clamp01(vc.input.Vertical);
                    if (vc.transmission.Shifting || userInput == 0 || FuelCutoff)
                        userInput = -1;

                    rpm += allowedRpmChange * userInput;
                }

                if(vc.transmission.Gear != 0 && vc.Speed > vc.transmission.GetMaxSpeedForGear(vc.transmission.Gear))
                {
                    StartFuelCutoff();
                }
            }          
            else
            {
                rpmOverflow = 0;
                rpm = 0;
                prevRpm = 0;
            }

            // Calculate power
            prevThrottle = throttle;
            power = 0;

            if (!starting && !stopping && IsRunning && !FuelCutoff && !vc.transmission.Shifting)
            {
                throttle = vc.input.Vertical;

                // Smooth only on throttle
                if (throttle > prevThrottle)
                {
                    throttle = Mathf.SmoothDamp(prevThrottle, throttle, ref throttleVelocity, throttleSmoothing);
                }
                else
                {
                    throttleVelocity = 0;
                }

                float directionInversion = vc.transmission.transmissionType == Transmission.TransmissionType.Manual ? Mathf.Sign(vc.transmission.Gear) : 1f;
                float userInput = Mathf.Clamp01(throttle * Mathf.Sign(vc.transmission.Gear) * directionInversion);

                power = Mathf.Abs(powerCurve.Evaluate(RPM / maxRPM) * maxPower * userInput)
                    * (1f - TotalPowerReduction) * forcedInduction.PowerGainMultiplier;

                // Reduce power if damaged
                if (vc.damage.enabled && vc.damage.performanceDegradation)
                    power *= (1f - Mathf.Pow(vc.damage.DamagePercent, 2f) * 0.6f);
            }
        }

        private void StartFuelCutoff()
        {
            fuelCutoffStart = Time.realtimeSinceStartup;
        }

    }
}

