using UnityEngine;
using System.Collections;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Destroys skidmark object when distance to the vehicle becomes greater then distance threshold.
    /// </summary>
    public class SkidmarkDestroy : MonoBehaviour
    {
        public VehicleController parentVehicleController;
        public float distanceThreshold = 100f;

        void Start()
        {
            InvokeRepeating("Check", 5f, 4f);
        }

        void Check()
        {
            if (parentVehicleController == null)
            {
                Destroy(this.gameObject);
            }
            else if (Vector3.Distance(this.transform.position, parentVehicleController.transform.position) > distanceThreshold)
            {
                Destroy(this.gameObject);
            }
        }
    }
}

