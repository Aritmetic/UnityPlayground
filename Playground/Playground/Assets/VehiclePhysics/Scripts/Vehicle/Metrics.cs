using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Class for holding metrics such as odometer, top speed and drift time.
    /// All the speed values are in m/s. If you need the value in km/h or mph
    /// use UnitConverter functions.
    /// </summary>
    [System.Serializable]
    public class Metrics
    {
        public Metric odometer;
        public Metric topSpeed;
        public Metric averageSpeed;
        public Metric totalDriftTime;
        public Metric continousDriftTime;
        public Metric totalDriftDistance;
        public Metric continousDriftDistance;

        private float driftEndTime;
        private float driftTimeout = 0.75f;

        private VehicleController vc;

        [System.Serializable]
        public class Metric
        {
            public float value = 0f;

            public delegate float UpdateDelegate();

            public void Update(UpdateDelegate del, bool increment)
            {
                if (increment)
                    value += del();
                else
                    value = del();
            }

            public void Reset()
            {
                value = 0;
            }
        }

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

        public void Update()
        {
            // Odometer
            odometer.Update(delegate { return vc.Speed * Time.fixedDeltaTime; }, true);

            // Top speed
            topSpeed.Update(
                delegate
                {
                    if (vc.Speed > topSpeed.value)
                    {
                        return vc.Speed;
                    }
                    return topSpeed.value;
                }, false);

            // Average speed
            averageSpeed.Update(
                delegate
                {
                    return odometer.value / Time.realtimeSinceStartup;
                }, false);

            // Total drift time
            totalDriftTime.Update(
                delegate
                {
                    if(vc.DetectWheelSkid())
                        return Time.fixedDeltaTime;
                    return 0;
                }, true);

            // Continous drift time
            continousDriftTime.Update(
                delegate
                {
                    if (vc.DetectWheelSkid())
                    {
                        driftEndTime = Time.realtimeSinceStartup;
                        return Time.fixedDeltaTime;
                    }
                    else if (Time.realtimeSinceStartup < driftEndTime + driftTimeout)
                    {
                        return Time.fixedDeltaTime;
                    }
                    else
                    {
                        return -continousDriftTime.value;
                    }
                }, true);

            // Total drift distance
            totalDriftDistance.Update(
                delegate
                {
                    if (vc.DetectWheelSkid())
                        return Time.fixedDeltaTime * vc.Speed;
                    return 0;
                }, true);

            // Continous drift distance
            continousDriftDistance.Update(
                delegate
                {
                    if (vc.DetectWheelSkid())
                    {
                        driftEndTime = Time.realtimeSinceStartup;
                        return Time.fixedDeltaTime * vc.Speed;
                    }
                    else if (Time.realtimeSinceStartup < driftEndTime + driftTimeout)
                    {
                        return Time.fixedDeltaTime * vc.Speed;
                    }
                    else
                    {
                        return -continousDriftDistance.value;
                    }
                }, true);
        }
    }
}

