using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NWH.VehiclePhysics;
using NWH.WheelController3D;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Example script for setting up a vehicle at runtime - modify as/if needed.
    /// Sets up a vehicle from script at runtime.
    /// Model is required to already have body colliders set up, as well as wheels tagged with correct tag.
    /// Model also needs to have correct rotation (Z-forward, Y-up, X-right).
    /// Works only on vehicles with two wheels per axle. Number of axles is not limited.
    /// </summary>
    public class VehicleFromScript : MonoBehaviour
    {
        public string wheelTag = "Wheel";
        public string ignoreLayer = "WheelControllerIgnore";
        public float vehicleMass = 1300;
        public float wheelRadius = 0.3f;
        public float wheelWidth = 0.28f;

        private List<WheelController> wheelControllers;
        private Rigidbody rb;

        void Awake()
        {
            // Find the rigidbody.
            rb = gameObject.GetComponent<Rigidbody>();

            // No rigidbody found, add a new one.
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.mass = vehicleMass;
            }

            // Rigidbody could not be found, return.
            if (rb == null)
            {
                Debug.LogError("Rigidbody could not be found or added.");
                return;
            }

            // Find wheelControllers
            List<WheelController> wheelControllers = new List<WheelController>();
            wheelControllers = GetComponentsInChildren<WheelController>().ToList();

            // No wheelControllers found, create new.
            if (wheelControllers.Count == 0)
            {
                // Find wheel GameObjects
                foreach (Transform child in GetComponentsInChildren<Transform>())
                {
                    if (child.CompareTag(wheelTag))
                    {
                        // Add WheelController container
                        GameObject wheelControllerGO = new GameObject();
                        wheelControllerGO.name = "WC_" + child.name;
                        wheelControllerGO.transform.SetParent(child.parent);
                        wheelControllerGO.transform.position = child.transform.position;
                        wheelControllerGO.transform.rotation = child.transform.rotation;

                        // Add WheelController
                        WheelController wc = wheelControllerGO.AddComponent<WheelController>();
                        wc.Visual = child.gameObject;
                        wc.Parent = gameObject;
                        wc.radius = wheelRadius;
                        wc.tireWidth = wheelWidth;
                        wc.ScanIgnoreLayers = LayerMask.NameToLayer("WheelControllerIgnore");
                        wc.VehicleSide = wc.DetermineSide(wc.transform.position, transform);
                        wheelControllers.Add(wc);

                        // Adjust container y position
                        wheelControllerGO.transform.position = child.transform.position + transform.up * wc.springLength * 0.7f;
                    }
                }
            }

            // No wheel controllers could be created, return.
            if (wheelControllers.Count == 0)
            {
                Debug.LogError("WheelController components could not be added. Check if wheels are correctly tagged.");
                return;
            }

            // Set ignore layer to all colliders
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.gameObject.layer = LayerMask.NameToLayer("WheelControllerIgnore");
            }

            // Add vehicle controller
            VehicleController vc = gameObject.AddComponent<VehicleController>();
            vc.SetDefaults();
        }
    }
}

