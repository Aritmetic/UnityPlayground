using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NWH.VehiclePhysics 
{
    /// <summary>
    /// Handles gear changing and also torque and RPM transmission in both directions.
    /// </summary>
    [System.Serializable]
    public class Transmission
    {
        public enum TransmissionType { Manual, Automatic, AutomaticSequential }

        /// <summary>
        /// Determines in which way gears can be changed.
        /// Manual - gears can only be shifted by manual user input.
        /// Automatic - automatic gear changing. Allows for gear skipping (e.g. 3rd->5th) which can be useful in trucks and other high gear count vehicles.
        /// AutomaticSequential - automatic gear changing but only one gear at the time can be shifted (e.g. 3rd->4th)
        /// </summary>
        [ShowInTelemetry]
        [Tooltip("Manual - gears can only be shifted by manual user input. " +
                 "Automatic - automatic gear changing. Allows for gear skipping (e.g. 3rd->5th) which can be useful in trucks and other high gear count vehicles. " +
                 "AutomaticSequential - automatic gear changing but only one gear at the time can be shifted (e.g. 3rd->4th)")]
        public TransmissionType transmissionType = TransmissionType.AutomaticSequential;


        public enum ReverseType { Auto, DoubleTap }

        [Tooltip("Behavior when switching from neutral or forward gears to reverse gear. " +
            "Auto - if the vehicle speed is low enough and vertical input negative, transmission will shift to reverse. " + 
            "DoubleTap - once all the requirements exist for shifting into reverse, user has to release the button and press it again to shift," +
            " otherwise the vehicle will stand still in neutral with brakes applied.")]
        public ReverseType reverseType = ReverseType.Auto;

        public enum DifferentialType { Equal, Open, LimitedSlip, Locking }

        /// <summary>
        /// Differential(s) that determine how the torque will be split between axles. Each axle then has its own differential (check axle settings).
        /// </summary>
        [Tooltip("Differential(s) that determine how the torque will be split between axles. Each axle then has its own differential (check axle settings).")]
        public DifferentialType differentialType = DifferentialType.Equal;

        [Header("Gears")]

        /// <summary>
        /// List of forward gear ratios starting from 1st forward gear.
        /// </summary>
        [Tooltip("List of forward gear ratios starting from 1st forward gear.")]
        [SerializeField]
        private List<float> forwardGears = new List<float>() { 8f, 5.5f, 4f, 3f, 2.2f, 1.7f, 1.3f };

        /// <summary>
        /// List of reverse gear ratios starting from 1st reverse gear.
        /// </summary>
        [Tooltip("List of reverse gear ratios starting from 1st reverse gear.")]
        [SerializeField]
        private List<float> reverseGears = new List<float>() { -5f };

        /// <summary>
        /// Final gear multiplier. Each gear gets multiplied by this value.
        /// </summary>
        [Tooltip("Final gear multiplier. Each gear gets multiplied by this value.")]
        [ShowInSettings("finalGearRatio", 2500, 10000, 500)]
        public float gearMultiplier = 1;

        [Header("Shifting")]

        /// <summary>
        /// RPM at which automatic transmission will shift up. If dynamic shift point is enabled this value will change depending on load.
        /// </summary>
        [ShowInTelemetry]
        [Tooltip("RPM at which automatic transmission will shift up. If dynamic shift point is enabled this value will change depending on load.")]
        [ShowInSettings(2000, 10000, 250)]
        public float targetShiftUpRPM = 3600;

        /// <summary>
        /// RPM at which automatic transmission will shift down. If dynamic shift point is enabled this value will change depending on load.
        /// </summary>
        [Tooltip("RPM at which automatic transmission will shift down. If dynamic shift point is enabled this value will change depending on load.")]
        [ShowInTelemetry]
        [ShowInSettings(500, 3000, 250)]
        public float targetShiftDownRPM = 1400;

        /// <summary>
        /// If enabled transmission will adjust both shift up and down points to match current load.
        /// </summary>
        [Tooltip("If enabled transmission will adjust both shift up and down points to match current load.")]
        [ShowInTelemetry]
        public bool dynamicShiftPoint = true;

        /// <summary>
        /// Shift point will randobly vary by the following percent of it's value.
        /// </summary>
        [Tooltip("Shift point will randobly vary by the following percent of it's value.")]
        [Range(0f, 0.2f)]
        public float shiftPointRandomness = 0.05f;

        /// <summary>
        /// Time it takes transmission to shift between gears.
        /// </summary>
        [Tooltip("Time it takes transmission to shift between gears.")]
        [ShowInTelemetry]
        public float shiftDuration = 0.2f;

        /// <summary>
        /// Maximum percentage of shift duration that will be added or substracted to it. Default is 20% (0.2f).
        /// </summary>
        [Tooltip("Shift duration will randomly vary by the following percentage of it's value.")]
        [Range(0f, 0.5f)]
        public float shiftDurationRandomness = 0.2f;

        /// <summary>
        /// Time after shifting in which shifting can not be done again.
        /// </summary>
        [Tooltip("Time after shifting in which shifting can not be done again.")]
        public float postShiftBan = 0.5f;

        [Header("Clutch")]

        /// <summary>
        /// Will clutch be automatically operated or will user input be used to operate it?
        /// </summary>
        [Tooltip("Will clutch be automatically operated or will user input be used to operate it?")]
        [ShowInTelemetry]
        public bool automaticClutch = true;

        /// <summary>
        /// 0 for fully released and 1 for fully depressed predal.
        /// </summary>
        [Tooltip("0 for fully released and 1 for fully depressed predal.")]
        [Range(0, 1)]
        [ShowInTelemetry]
        public float clutchPedalPressedPercent;

        /// <summary>
        /// Describes how much clutch will 'grab' as the pedal is released. When the Y of the curve is at 1 this means that the clutch is fully engaged, i.e. there 
        /// is no slip between transmission and engine. When the Y of the curve is at 0 there is no connection between engine and transmission / wheels.
        /// 0 on the X axis represents fully released clutch pedal while 1 represents fully pressed pedal. Normally every clutch would have two points,
        /// [0,1] and [1,0], and the in-between will vary from vehicle to vehicle.
        /// </summary>
        [Tooltip("Describes how much clutch will 'grab' as the pedal is released. When the Y of the curve is at 1 this means that the clutch is fully engaged, i.e. there " +
            "is no slip between transmission and engine. When the Y of the curve is at 0 there is no connection between engine and transmission / wheels." +
            "0 on the X axis represents fully released clutch pedal while 1 represents fully pressed pedal. Normally every clutch would have two points," +
            "[0,1] and [1,0], and the in-between will vary from vehicle to vehicle.")]
        public AnimationCurve clutchEngagementCurve = new AnimationCurve(new Keyframe[2] {
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            });

        [Header("Automatic Clutch")]

        /// <summary>
        /// Engine will try and hold RPM at this value while the clutch is being released.
        /// </summary>
        [Tooltip("Engine will try and hold RPM at this value while the clutch is being released.")]
        [ShowInTelemetry, ShowInSettings(1000, 2500, 100)]
        public float targetClutchRPM = 1500;

        /// <summary>
        /// Time since the start of the scene when the last shift happened.
        /// </summary>
        [HideInInspector]
        public float lastShiftTime;

        private float addedClutchRPM;

        /// <summary>
        /// List of gears starting with reverse gears, neutral and then forward gears. List is constructed on initialization from forward and reverse gear lists.
        /// This is done so that things like Gear++ and Gear-- can be done.
        /// </summary>
        private List<float> gears = new List<float>();

        private VehicleController vc;

        [ShowInSettings(0.01f, 0.5f, 0.1f)]
        private float initialShiftDuration;
        private float randomShiftDownPointAddition;
        private float randomShiftUpPointAddition;

        // Self-adjusting shift points
        [ShowInTelemetry]
        private float adjustedShiftUpRpm;
        [ShowInTelemetry]
        private float adjustedShiftDownRpm;

        private float smoothedVerticalInput;
        private float verticalInputChangeVelocity;
        private int gear = 0;

        private bool allowedToReverse;
        private bool reverseHasBeenPressed = false;


        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
            ReconstructGearList();
            initialShiftDuration = shiftDuration;
            UpdateRandomShiftDuration();
            UpdateRandomShiftPointAddition();
        }


        public void Update()
        {
            if (transmissionType == TransmissionType.Manual)
            {
                ManualShift();
            }
            else if (transmissionType == TransmissionType.Automatic || transmissionType == TransmissionType.AutomaticSequential)
            {
                AutomaticShift();
            }
        }

        /// <summary>
        /// Updates clutch state.
        /// </summary>
        public void UpdateClutch()
        {
            addedClutchRPM = 0f;
            if (automaticClutch)
            {
                clutchPedalPressedPercent = 0;
                // Only apply clutch when accelerating
                if ( (vc.transmission.transmissionType == TransmissionType.Automatic) && (Gear == -1 && vc.input.Vertical < 0)
                    || (vc.transmission.transmissionType == TransmissionType.Manual) && (Gear == -1 && vc.input.Vertical > 0)
                    || (Gear == 1 && vc.input.Vertical > 0))
                {
                    clutchPedalPressedPercent = Mathf.Clamp01((targetClutchRPM - Mathf.Abs(ReverseRPM)) / targetClutchRPM) * Mathf.Abs(vc.input.Vertical);
                }
                addedClutchRPM = (1f - GetClutchEngagementAtPedalPosition(clutchPedalPressedPercent)) * targetClutchRPM;
            }
            else
            {
                clutchPedalPressedPercent = vc.input.Clutch;
                addedClutchRPM = (1f - GetClutchEngagementAtPedalPosition(clutchPedalPressedPercent)) * (vc.engine.maxRPM - vc.engine.minRPM) * Mathf.Abs(vc.input.Vertical);
            }
        }


        /// <summary>
        /// Indicates the state the clutch is in.
        /// 0 - clutch is released and engine is connected to the wheels.
        /// 1 - clutch is pressed and engine is free revving.
        /// </summary>
        [ShowInTelemetry]
        public float ClutchPercent
        {
            get
            {
                return clutchPedalPressedPercent;
            }
        }


        public float AddedClutchRPM
        {
            get
            {
                return addedClutchRPM;
            }
        }


        /// <summary>
        /// Number of forward gears.
        /// </summary>
        public int ForwardGearCount
        {
            get
            {
                return gears.Count - 1 - reverseGears.Count;
            }
        }

        /// <summary>
        /// Number of reverse gears.
        /// </summary>
        public int ReverseGearCount
        {
            get
            {
                return reverseGears.Count;
            }
        }

        /// <summary>
        /// List of forward gears. Gears list will be updated if new value is assigned.
        /// </summary>
        public List<float> ForwardGears
        {
            get
            {
                return forwardGears;
            }
            set
            {
                forwardGears = value;
                ReconstructGearList();
            }
        }

        /// <summary>
        /// List of reverse gears. Gears list will be updated if new value is assigned.
        /// </summary>
        public List<float> ReverseGears
        {
            get
            {
                return reverseGears;
            }
            set
            {
                reverseGears = value;
                ReconstructGearList();
            }
        }

        /// <summary>
        /// Returns maximum speed for the engine's maxRPM and gear ratio.
        /// </summary>
        public float GetMaxSpeedForGear(int g)
        {
            if (Gear == 0) return Mathf.Infinity;

            float wheelRadiusSum = 0;
            int wheelCount = 0;

            foreach(Wheel wheel in vc.Wheels)
            {
                wheelRadiusSum += wheel.Radius;
                wheelCount++;
            }

            if(wheelCount > 0)
            {
                float avgWheelRadius = wheelRadiusSum / wheelCount;
                float maxRpmForGear = TransmitRPM(vc.engine.maxRPM);
                float maxSpeed = avgWheelRadius * maxRpmForGear * 0.105f;
                return maxSpeed;
            }
            return 0;
        }

        /// <summary>
        /// Engine RPM at which transmission will shift up if dynamic shift point is enabled.
        /// </summary>
        public float AdjustedShiftUpRpm
        {
            get
            {
                return adjustedShiftUpRpm;
            }
        }

        /// <summary>
        /// Engine RPM at which transmission will shift down if dynamic shift point is enabled.
        /// </summary>
        public float AdjustedShiftDownRpm
        {
            get
            {
                return adjustedShiftDownRpm;
            }
        }

        /// <summary>
        /// 0 for neutral, less than 0 for reverse gears and lager than 0 for forward gears.
        /// </summary>
        public int Gear
        {
            get
            {
                if (gear < 0 - reverseGears.Count)
                    return gear = 0 - reverseGears.Count;
                else if (gear >= gears.Count - reverseGears.Count - 1)
                    return gear = gears.Count - reverseGears.Count - 1;
                else
                    return gear;
            }
            set
            {
                if (value < 0 - reverseGears.Count)
                    gear = 0 - reverseGears.Count;
                else if (value >= gears.Count - reverseGears.Count - 1)
                    gear = ForwardGearCount;
                else
                    gear = value;
            }
        }

        /// <summary>
        /// Returns current gear name as a string, e.g. "R", "R2", "N" or "1"
        /// </summary>
        [ShowInTelemetry]
        public string GearName
        {
            get
            {
                if (Gear == 0)
                {
                    return "N";
                }
                else if (Gear > 0)
                {
                    return Gear.ToString();
                }
                else
                {
                    if(reverseGears.Count > 1)
                    {
                        return "R" + Mathf.Abs(Gear).ToString();
                    }
                    else
                    {
                        return "R";
                    }
                }

            }
        }

        /// <summary>
        /// List of all gear ratios including reverse, forward and neutral gears. e.g. -2nd, -1st, 0 (netural), 1st, 2nd, 3rd, etc.
        /// </summary>
        public List<float> Gears
        {
            get
            {
                return gears;
            }
        }

        /// <summary>
        /// Total gear ratio of the transmission for current gear.
        /// </summary>
        [ShowInTelemetry]
        public float GearRatio
        {
            get
            {
                return gears[GearToIndex(gear)] * gearMultiplier;
            }
        }

        /// <summary>
        /// Total gear ratio of the transmission for the specific gear.
        /// </summary>
        /// <returns></returns>
        public float GetGearRatio(int g)
        {
            return gears[GearToIndex(g)] * gearMultiplier;
        }


        /// <summary>
        /// Returns clutch value in range of 1 (disconnected) to 0 (connected) for the passed pedal travel.
        public float GetClutchEngagementAtPedalPosition(float clutchPercent)
        {
            return clutchEngagementCurve.Evaluate(clutchPercent);
        }


        /// <summary>
        /// RPM at the axle side.
        /// </summary>
        public float RPM
        {
            get
            {
                float rpmSum = 0;
                foreach (Axle axle in vc.axles)
                    rpmSum += axle.RPM;
                return rpmSum / vc.axles.Count;
            }
        }

        /// <summary>
        /// RPM at the engine side calculated from the RPM at the axle side and gear ratios.
        /// </summary>
        public float ReverseRPM
        {
            get
            {
                return ReverseTransmitRPM(RPM);
            }
        }


        /// <summary>
        /// Recreates gear list from the forward and reverse gears lists.
        /// </summary>
        public void ReconstructGearList()
        {
            // Construct gear list
            List<float> reversedReverseGears = reverseGears;
            reversedReverseGears.Reverse();
            gears.Clear();
            gears.AddRange(reversedReverseGears);
            gears.Add(0);
            gears.AddRange(forwardGears);
        }

        /// <summary>
        /// Distribute torque between axles.
        /// </summary>
        /// <param name="torque">Input torque</param>
        /// <param name="topRPM">Input RPM</param>
        public void TorqueSplit(float torque, float topRPM)
        {
            // 0 torque on reverse direction
            if (transmissionType == TransmissionType.Automatic || transmissionType == TransmissionType.AutomaticSequential)
            {
                if ((Gear < 0 && vc.input.Vertical > 0) || (Gear >= 0 && vc.input.Vertical < 0) || Gear == 0)
                {
                    torque = 0;
                }
            }
            else if (transmissionType == TransmissionType.Manual)
            {
                if (vc.input.Vertical < 0)
                    torque = 0;
            }

            // Lower torque on clutch depression
            float torqueClutchMod = 1f;
            if (!automaticClutch)
            {
                torqueClutchMod = GetClutchEngagementAtPedalPosition(clutchPedalPressedPercent);
            }
            torque = torque * Mathf.Sign(Gear) * torqueClutchMod;

            // Non-tracked vehicle torque split
            if (!vc.tracks.trackedVehicle)
            {
                float powerCoefficientSum = 0;
                int poweredAxleCount = 0;

                // Calculate sum of all power coefficients on all axles
                foreach (Axle axle in vc.axles)
                {
                    powerCoefficientSum += axle.powerCoefficient;
                    if (axle.IsPowered) poweredAxleCount++;
                }

                // Reset biases
                foreach (Axle axle in vc.axles)
                {
                    axle.Bias = 0;
                }

                // Torque slip for limited slip and locking differentials
                if (differentialType == DifferentialType.LimitedSlip || differentialType == DifferentialType.Locking)
                {
                    float rpmSum = 0;
                    float biasSum = 0;
                     
                    foreach (Axle axle in vc.axles)
                        rpmSum += axle.SmoothRPM;

                    foreach (Axle axle in vc.axles)
                    {
                        axle.Bias = rpmSum == 0 ? 0 : 1f - (axle.SmoothRPM / rpmSum);
                        if (differentialType == DifferentialType.Locking)
                            axle.Bias = Mathf.Pow(axle.Bias, 8f);
                        biasSum += axle.Bias * axle.powerCoefficient;
                    }

                    foreach (Axle axle in vc.axles)
                        axle.Bias = biasSum == 0 ? 0 : (axle.Bias * axle.powerCoefficient) / biasSum;
                }
                // Split torque between axles only based on power coeficient.
                else if (differentialType == DifferentialType.Open)
                {
                    float rpmSum = 0;

                    foreach (Axle axle in vc.axles)
                    {
                        rpmSum += axle.SmoothRPM * axle.powerCoefficient;
                    }

                    foreach (Axle axle in vc.axles)
                    {
                        axle.Bias = rpmSum == 0 ? 0 : (axle.SmoothRPM * axle.powerCoefficient) / rpmSum;
                    }
                }
                else if (differentialType == DifferentialType.Equal)
                {
                    foreach (Axle axle in vc.axles)
                    {
                        axle.Bias = powerCoefficientSum == 0 ? 0 : axle.powerCoefficient / powerCoefficientSum;
                    }
                }

                // Split torque based on axle bias
                foreach (Axle axle in vc.axles)
                    if (axle.IsPowered)
                    {
                        axle.TorqueSplit(torque * axle.Bias, topRPM);
                    }
            }
            // Tracked vehicle torque split
            else
            {
                int leftCount = 0;
                int rightCount = 0;

                foreach(Axle axle in vc.axles)
                {
                    if(axle.leftWheel.IsGrounded)
                    {
                        leftCount++;
                    }
                    if(axle.rightWheel.IsGrounded)
                    {
                        rightCount++;
                    }
                }

                float leftTorque = torque * 0.5f;
                float rightTorque = torque * 0.5f;

                float leftPerWheelTorque = leftCount == 0 ? 0 : leftTorque / leftCount;
                float rightPerWheelTorque = rightCount == 0 ? 0 : rightTorque / rightCount;

                foreach(Axle axle in vc.axles)
                {
                    if(axle.leftWheel.IsGrounded)
                    {
                        axle.leftWheel.MotorTorque = leftPerWheelTorque;
                    }
                    if(axle.rightWheel.IsGrounded)
                    {
                        axle.rightWheel.MotorTorque = rightPerWheelTorque;
                    }

                }
            }
        }

        /// <summary>
        /// Shifts into given gear. 0 for neutral, less than 0 for reverse and above 0 for forward gears.
        /// </summary>
        public void ShiftInto(int g, bool startTimer = true)
        {
            if (CanShift)
            {
                int prevGear = Gear;
                Gear = g;
                if (Gear != 0 && Gear != prevGear)
                {
                    UpdateRandomShiftDuration();
                    UpdateRandomShiftPointAddition();
                    if(startTimer) StartTimer();
                }
            }
        }

        /// <summary>
        /// True if shifting is allowed at the moment.
        /// </summary>
        public bool CanShift
        {
            get
            {
                if (Time.realtimeSinceStartup > lastShiftTime + shiftDuration + postShiftBan)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// True if currently shifting.
        /// </summary>
        public bool Shifting
        {
            get
            {
                if(Time.realtimeSinceStartup < lastShiftTime + shiftDuration)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Converts engine torque to axle torque.
        /// </summary>
        public float TransmitTorque(float inputTorque)
        {
            float gearRatio = GearRatio;
            return gearRatio == 0 ? 0 : Mathf.Abs(inputTorque * gearRatio);
        }


        /// <summary>
        /// Converts axle torque to engine torque.
        /// </summary>
        public float ReverseTransmitTorque(float inputTorque)
        {
            float gearRatio = GearRatio;
            return gearRatio == 0 ? 0 : Mathf.Abs(inputTorque / GearRatio);
        }

        /// <summary>
        /// Converts engine RPM to axle RPM.
        /// </summary>
        public float TransmitRPM(float inputRPM)
        {
            if(GearRatio != 0)
                return Mathf.Abs(inputRPM / GearRatio);
            return 0;
        }

        /// <summary>
        /// Converts axle RPM to engine RPM.
        /// </summary>
        public float ReverseTransmitRPM(float inputRPM)
        {
            return Mathf.Abs(inputRPM * GearRatio);
        }

        /// <summary>
        /// Converts axle RPM to engine RPM for given gear in Gears list.
        /// </summary>
        public float ReverseTransmitRPM(float inputRPM, int g)
        {
            return Mathf.Abs(inputRPM * gears[GearToIndex(g)] * gearMultiplier);
        }


        private float GetMaxGearRatio()
        {
            float max = 0;
            for(int i = 0; i < forwardGears.Count; i++)
            {
                if (forwardGears[i] > max) max = forwardGears[i];
            }
            return max;
        }


        private float GetMinGearRatio()
        {
            float min = Mathf.Infinity;
            for (int i = 0; i < forwardGears.Count; i++)
            {
                if (forwardGears[i] < min) min = forwardGears[i];
            }
            return min;
        }

        private float GetGearRatioRange()
        {
            return GetMaxGearRatio() - GetMinGearRatio();
        }


        private void StartTimer()
        {
            lastShiftTime = Time.realtimeSinceStartup;
        }


        private int GearToIndex(int g)
        {
            return g + reverseGears.Count;
        }


        private void ManualShift()
        {
            if (vc.input.ShiftUp)
            {
                if (!(Gear == 0 && vc.Speed < -2f))
                {
                    Gear++;
                }
            }
            if (vc.input.ShiftDown)
            {
                if (!(Gear == 0 && vc.Speed > 2f))
                {
                    Gear--;
                }
            }


            // Lower the shift flags after the shift has been processed.
            vc.input.ShiftUp = false;
            vc.input.ShiftDown = false;
        }


        /// <summary>
        /// Handles automatic and automatic sequential shifting.
        /// Engine RPM is used when checking for downshift and clutchless engine RPM when checking for upshift 
        /// to prevent added RPM from clutch triggering an upshift / downshift loop.
        /// </summary>
        private void AutomaticShift()
        {
            if (reverseType == ReverseType.Auto)
            {
                allowedToReverse = true;
            }
            else if(reverseType == ReverseType.DoubleTap)
            {
                allowedToReverse = reverseHasBeenPressed ? false : true;

                if (vc.input.Vertical < -0.05f)
                {
                    reverseHasBeenPressed = true;
                }
                else
                {
                    reverseHasBeenPressed = false;
                }
            }

            float damping = Mathf.Abs(vc.input.Vertical) > smoothedVerticalInput ? 0.3f : 5f;
            smoothedVerticalInput = Mathf.SmoothDamp(smoothedVerticalInput, Mathf.Abs(vc.input.Vertical), ref verticalInputChangeVelocity, damping);

            adjustedShiftDownRpm = targetShiftDownRPM + randomShiftDownPointAddition;
            adjustedShiftUpRpm = targetShiftUpRPM + randomShiftUpPointAddition;

            if (dynamicShiftPoint)
            {
                adjustedShiftDownRpm = targetShiftDownRPM + (-0.5f + smoothedVerticalInput) * vc.engine.minRPM * 0.4f;
                adjustedShiftUpRpm = targetShiftUpRPM + (-0.5f + smoothedVerticalInput) * vc.engine.maxRPM * 0.4f;

                float inclineModifier = Vector3.Dot(vc.transform.forward, Vector3.up);
                adjustedShiftDownRpm += vc.engine.maxRPM * inclineModifier;
                adjustedShiftUpRpm += vc.engine.maxRPM * inclineModifier;

                adjustedShiftUpRpm = Mathf.Clamp(adjustedShiftUpRpm, targetShiftUpRPM, vc.engine.maxRPM * 0.95f);
                adjustedShiftDownRpm = Mathf.Clamp(adjustedShiftDownRpm, vc.engine.minRPM * 1.2f, targetShiftDownRPM * 1.5f);
            }

            // In neutral
            if (Gear == 0)
            {
                // Shift into first
                if (vc.input.Vertical > 0f && vc.ForwardVelocity >= -1f)
                {
                    Gear = 1;
                }
                // Shift into reverse
                else if (vc.input.Vertical < 0f && vc.ForwardVelocity <= 1f && allowedToReverse)
                {
                    Gear = -1;
                }
            }
            // In reverse
            else if (Gear < 0)
            {
                // Shift into 1st
                if (vc.input.Vertical > 0f && vc.Speed < 1f)
                {
                    ShiftInto(1, false);
                }
                // Shift into neutral
                else if (vc.input.Vertical == 0f && vc.Speed < 1f)
                {
                    ShiftInto(0, false);
                }

                // Reverse upshift
                if (vc.engine.RPM > AdjustedShiftUpRpm)
                {
                    ShiftInto(Gear - 1);
                }
                // Reverse downshift
                else if (vc.engine.RPM < adjustedShiftDownRpm)
                {
                    // 1st reverse gear to neutral
                    if(Gear == -1 && (vc.input.Vertical == 0 || vc.engine.RpmOverflow < -(vc.engine.minRPM / 5f)))
                    {
                        ShiftInto(0);
                    }
                    // To first gear
                    else if(Gear < -1)
                    {
                        ShiftInto(Gear + 1);
                    }
                }
            }
            // In forward
            else
            {
                if (vc.ForwardVelocity > 0.2f)
                {
                    // Upshift
                    if (vc.engine.RPM > adjustedShiftUpRpm && vc.input.Vertical >= 0 && !vc.WheelSpin)
                    {
                        bool grounded = true;
                        foreach (Wheel wheel in vc.Wheels)
                        {
                            if (!wheel.IsGrounded)
                            {
                                grounded = false;
                                break;
                            }
                        }

                        // Upshift
                        if (grounded)
                        {
                            if (transmissionType == TransmissionType.Automatic)
                            {
                                int g = Gear;
                                while (g < forwardGears.Count - 1)
                                {
                                    float wouldBeEngineRpm = ReverseTransmitRPM(RPM, g);
                                    if (wouldBeEngineRpm > adjustedShiftDownRpm && wouldBeEngineRpm < (adjustedShiftDownRpm + adjustedShiftUpRpm) / 2f)
                                    {
                                        break;
                                    }

                                    g++;
                                    wouldBeEngineRpm = ReverseTransmitRPM(RPM, g);
                                    if (wouldBeEngineRpm < adjustedShiftDownRpm)
                                    {
                                        g--;
                                        break;
                                    }
                                }
                                if (g != Gear)
                                {
                                    ShiftInto(g);
                                }
                            }
                            else
                            {
                                ShiftInto(Gear + 1);
                            }

                        }
                    }
                    else if (vc.engine.RPM < adjustedShiftDownRpm)
					{
                        // Check if downshift allowed on tracked vehicle
                        if (vc.tracks.trackedVehicle)
                        {
                            int notGroundedCount = 0;
                            foreach (Wheel wheel in vc.Wheels)
                            {
                                if (!wheel.IsGrounded)
                                {
                                    notGroundedCount++;
                                }
                            }
                            if (notGroundedCount > vc.Wheels.Count * 0.4f)
                            {
                                return;
                            }
                        }

                        // Non-sequential
                        if (transmissionType == TransmissionType.Automatic)
                        {
                            if (Gear != 1)
                            {
                                int g = Gear;
                                while (g > 1)
                                {
                                    g--;
                                    float wouldBeEngineRpm = ReverseTransmitRPM(RPM, g);
                                    if (wouldBeEngineRpm > adjustedShiftUpRpm)
                                    {
                                        g++;
                                        break;
                                    }
                                }
                                if (g != Gear)
                                {
                                    ShiftInto(g);
                                }
                            }
                            else if (vc.Speed < 0.8f && Mathf.Abs(vc.input.Vertical) < 0.05f)
                            {
                                ShiftInto(0);
                            }
                        }
                        // Sequential
                        else
                        {
                            if (Gear != 1)
                            {
                                ShiftInto(Gear - 1);
                            }
                            else if (vc.Speed < 0.8f && Mathf.Abs(vc.input.Vertical) < 0.05f)
                            {
                                ShiftInto(0);
                            }
                        }
					}
                }
                // Shift into reverse
                else if (vc.ForwardVelocity <= 0.2f && vc.input.Vertical < -0.05f && allowedToReverse) 
                {
                    ShiftInto(-1, false);
                }
                // Shift into neutral
                else if (vc.input.Vertical == 0 || (!allowedToReverse && vc.input.Vertical < 0) || vc.engine.RpmOverflow < -(vc.engine.minRPM / 5f))
                {
                    ShiftInto(0);
                }
            }
        }

        private void UpdateRandomShiftDuration()
        {
            shiftDuration = initialShiftDuration + Random.Range(-shiftDurationRandomness * initialShiftDuration, shiftDurationRandomness * initialShiftDuration);
        }

        private void UpdateRandomShiftPointAddition()
        {
            randomShiftDownPointAddition = Random.Range(-shiftPointRandomness * targetShiftDownRPM, shiftPointRandomness * targetShiftDownRPM);
            randomShiftUpPointAddition = Random.Range(-shiftPointRandomness * targetShiftUpRPM, shiftPointRandomness * targetShiftUpRPM);
        }
    } 
}
