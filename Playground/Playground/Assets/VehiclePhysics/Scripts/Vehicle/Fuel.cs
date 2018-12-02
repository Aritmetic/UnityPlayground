using UnityEngine;
using System.Collections;

namespace NWH.VehiclePhysics
{
    [System.Serializable]
    public class Fuel
    {
        /// <summary>
        /// Should fuel be used? If set to false HasFuel will always return true.
        /// </summary>
        [Tooltip("Should fuel be used?")]
        public bool useFuel = false;

        /// <summary>
        /// Fuel capacity in liters.
        /// </summary>
        [Tooltip("Fuel capacity in liters.")]
        public float capacity = 50f;

        /// <summary>
        /// Maximum amount in liters that the fuel tank can hold.
        /// </summary>
        [Tooltip("Maximum amount in liters that the fuel tank can hold.")]
        public float amount = 50f;

        /// <summary>
        /// Engine efficiency (in percent). 1 would mean that all the energy contained in fuel would go into output power.
        /// </summary>
        [Tooltip("Engine efficiency (in percent). 1 would mean that all the energy contained in fuel would go into output power.")]
        public float efficiency = 0.45f;
        
        private float unitConsumption;
        private float consumptionPerHour;
        private float maxConsumptionPerHour = 20f;
        private float distanceTraveled = 0f;
        private float measuredConsumption = 0f;

        private VehicleController vc;

        /// <summary>
        /// True if has fuel or if use fuel is false.
        /// </summary>
        public bool HasFuel
        {
            get
            {
                if (!useFuel)
                {
                    return true;
                }
                else
                {
                    if(amount > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Percentage of fuel from the max amount the tank can hold.
        /// </summary>
        public float FuelPercentage
        {
            get
            {
                return Mathf.Clamp01(amount / capacity);
            }
        }

        /// <summary>
        /// Fuel consumption in liters per second.
        /// </summary>
        public float ConsumptionLitersPerSecond
        {
            get
            {
                return consumptionPerHour / 3600f;
            }
        }

        /// <summary>
        /// Fuel consumption in miles per galon.
        /// </summary>
        public float ConsumptionMPG
        {
            get
            {
                return UnitConverter.L100kmToMpg(unitConsumption);
            }
        }

        /// <summary>
        /// Fuel consumption in liters per 100 kilometers.
        /// </summary>
        public float ConsumptionLitersPer100Kilometers
        {
            get
            {
                return unitConsumption;
            }
        }

        /// <summary>
        /// Fuel consumption in kilometers per liter.
        /// </summary>
        public float ConsumptionKilometersPerLiter
        {
            get
            {
                return UnitConverter.L100kmToKml(unitConsumption);
            }
        }

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
        }

        public void Update()
        {
            if(useFuel && vc.engine.IsRunning)
            {
                // Assuming fuel has 36 MJ/L. 1KWh = 3.6MJ. 1L = 36MJ = 10kWH.
                maxConsumptionPerHour = (vc.engine.maxPower / 10f) * (1f - efficiency);

                consumptionPerHour = (vc.engine.Power / vc.engine.maxPower) * maxConsumptionPerHour;
                consumptionPerHour = Mathf.Clamp(consumptionPerHour, maxConsumptionPerHour * 0.01f, Mathf.Infinity);

                // Reduce fuel amount until empty.
                amount -= (consumptionPerHour / 3600) * Time.fixedDeltaTime;
                amount = Mathf.Clamp(amount, 0f, capacity);

                if(amount == 0 && vc.engine.IsRunning)
                {
                    vc.engine.Stop();
                }

                // Calculate consumption per distance for mpg, km/l and l/100km values.
                distanceTraveled = vc.Speed * Time.fixedDeltaTime;
                measuredConsumption = (consumptionPerHour / 3600f) * Time.fixedDeltaTime;

                float perHour = 3600f / Time.fixedDeltaTime;
                float measuredConsPerHour = measuredConsumption * perHour;
                float measuredDistPerHour = (distanceTraveled * perHour) / 100000f;
                unitConsumption = measuredDistPerHour == 0 ? 0 : Mathf.Clamp(measuredConsPerHour / measuredDistPerHour, 0f, 99.9f);
            }
            else
            {
                consumptionPerHour = 0f;
                unitConsumption = 0;
            }
        }
    }
}

