using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Used for chaning vehicles. Also activates and deactivates vehicle cameras if default VehicleCamera system is used.
    /// </summary>
    [DisallowMultipleComponent]
    public class VehicleChanger : MonoBehaviour
    {
        /// <summary>
        /// List of all of the vehicles that can be selected and driven in the scene.
        /// </summary>
        [Tooltip("List of all of the vehicles that can be selected and driven in the scene. " +
            "If set to 0 script will try to auto-find all the vehicles in the scene with a tag define by VehiclesTag parameter.")]
        [SerializeField]
        public List<VehicleController> vehicles = new List<VehicleController>();

        /// <summary>
        /// Index of the current vehicle in vehicles list.
        /// </summary>
        [Tooltip("Index of the current vehicle. Set it to the vehicle you want your scene to start with.")]
        private int currentIndex;

        /// <summary>
        /// Tag that the script will search for if vehicles list is empty. Can be left empty if vehicles have already been assigned manually.
        /// </summary>
        [Tooltip("Tag that the script will search for if vehicles list is empty. Can be left empty if vehicles have already been assigned manually.")]
        public string vehicleTag = "Vehicle";

        private bool deactivateAll = false;
        private bool characterBased = false;


        /// <summary>
        /// Returns current selected vehicle.
        /// </summary>
        public VehicleController CurrentVehicle
        {
            get
            {
                return vehicles[currentIndex];
            }
        }


        void Awake()
        {
            if(vehicles.Count == 0)
            {
                FindVehicles();
            }

            if(DeactivateAll)
            {
                DeactivateAllIncludingActive();
            }
            else
            {
                DeactivateAllExceptActive();
            }
        }


        void Update()
        {
            if(!CharacterBased && Input.GetButtonDown("ChangeVehicle"))
            {
                NextVehicle();
            }

            if(DeactivateAll)
            {
                DeactivateAllIncludingActive();
            }
        }


        void FindVehicles()
        {
            var vehicleObjs = GameObject.FindGameObjectsWithTag(vehicleTag);
            foreach (GameObject go in vehicleObjs)
            {
                VehicleController vc = null;
                if (vc = go.GetComponent<VehicleController>())
                {
                    if (!vc.trailer.isTrailer)
                    {
                        if (vc.gameObject.activeInHierarchy)
                        {
                            if (!vehicles.Contains(vc)) vehicles.Add(vc);
                        }
                        else
                        {
                            if (vehicles.Contains(vc)) vehicles.Remove(vc);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Finds nearest vehicle on the vehicles list.
        /// </summary>
        public VehicleController NearestVehicleFrom(GameObject go)
        {
            VehicleController nearest = null;

            int minIndex = -1;
            float minDist = Mathf.Infinity;
            if (vehicles.Count > 0)
            {
                for(int i = 0; i < vehicles.Count; i++)
                {
                    if (!vehicles[i].gameObject.activeInHierarchy) continue;

                    float distance = Vector3.Distance(go.transform.position, vehicles[i].transform.position);
                    if(distance < minDist)
                    {
                        minIndex = i;
                        minDist = distance;
                    }
                }
                nearest = vehicles[minIndex];
            }

            return nearest;
        }

        /// <summary>
        /// Changes vehicle to a next vehicle on the Vehicles list.
        /// </summary>
        public void NextVehicle()
        {
            ChangeVehicle(currentIndex + 1);
        }

        /// <summary>
        /// Changes vehicle to a vehicle with the requested name if there is such a vehicle.
        /// </summary>
        public void ChangeVehicle(VehicleController vc)
        {
            for(int i = 0; i < vehicles.Count; i++)
            {
                if(vehicles[i] == vc)
                {
                    ChangeVehicle(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Changes vehicle to requested vehicle.
        /// </summary>
        /// <param name="index">Index of a vehicle in Vehicles list.</param>
        public void ChangeVehicle(int index)
        {
            currentIndex = index;
            if (currentIndex >= vehicles.Count)
            {
                currentIndex = 0;
            }
            DeactivateAllExceptActive();
        }

        /// <summary>
        /// Returns currently active vehicle controller or null if none are active.
        /// </summary>
        public VehicleController ActiveVehicleController
        {
            get
            {
                if (!DeactivateAll)
                    return vehicles[currentIndex];
                else
                    return null;
            }
        }


        public bool DeactivateAll
        {
            get
            {
                return deactivateAll;
            }

            set
            {
                deactivateAll = value;
            }
        }


        public bool CharacterBased
        {
            get
            {
                return characterBased;
            }

            set
            {
                characterBased = value;
            }
        }

        void DeactivateAllExceptActive()
        {
            for (int i = 0; i < vehicles.Count; i++)
            {
                if (i == currentIndex && !DeactivateAll)
                {
                    vehicles[i].Active = true;
                    SetCameras(vehicles[i], true);
                }
                else
                {
                    vehicles[i].Active = false;
                    SetCameras(vehicles[i], false);
                }
            }
        }

        void DeactivateAllIncludingActive()
        {
            for (int i = 0; i < vehicles.Count; i++)
            {
                vehicles[i].Active = false;
                SetCameras(vehicles[i], false);
            }
        }


        void SetCameras(VehicleController vc, bool state)
        {
            var cameras = vc.gameObject.GetComponentsInChildren<Camera>(true);
            foreach(Camera camera in cameras)
            {
                camera.gameObject.SetActive(state);
            }
        }

    }

}
