using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Switches between the camera objects that are children to this object and contain camera tag, 
    /// in order they appear in the hierarchy or in order they are added to the vehicle cameras list.
    /// </summary>
    public class CameraChanger : MonoBehaviour
    {
        /// <summary>
        /// Index of the camera from vehicle cameras list that will be active first.
        /// </summary>
        [Tooltip("Index of the camera from vehicle cameras list that will be active first.")]
        public int currentCamera = 0;

        /// <summary>
        /// Cameras with this tag will be added to the vehicle cameras list. Cameras need to be children of this object.
        /// </summary>
        [Tooltip("Cameras with this tag will be added to the vehicle cameras list. Cameras need to be children of this object.")]
        public string cameraTag = "VehicleCamera";

        /// <summary>
        /// List of cameras that the changer will cycle through. Leave empty if you want cameras to be automatically detected.
        /// To be detected cameras need to have camera tag and be children of the object this script is attached to.
        /// </summary>
        [Tooltip("List of cameras that the changer will cycle through. Leave empty if you want cameras to be automatically detected." +
            " To be detected cameras need to have camera tag and be children of the object this script is attached to.")]
        public List<GameObject> vehicleCameras = new List<GameObject>();

        private int previousCamera = 0;
        private VehicleController vehicleController;
        private bool wasActive;
        private CameraInsideVehicle cis = null;

        void Awake()
        {
            vehicleController = GetComponentInParent<VehicleController>();

            if (vehicleController == null)
                Debug.Log("None of the parents of camera changer contain VehicleController component. " +
                 "Make sure that the camera changer is amongst the children of VehicleController object.");

            if(vehicleCameras.Count == 0)
            {
                foreach(Camera cam in GetComponentsInChildren<Camera>(true))
                {
                    if(cam.transform.CompareTag(cameraTag))
                    {
                        vehicleCameras.Add(cam.gameObject);
                    }
                }
            }

            if (vehicleCameras.Count == 0)
                Debug.LogWarning("No cameras could be found with camera tag. Either add cameras manually or " +
                    "add them as children to the game object this script is attached to.");

            if (!vehicleController.Active)
            {
                DisableAllCameras();
            }
            else
            {
                DisableAllCamerasExceptCurrent();
            }

            wasActive = vehicleController.Active;
        }

        void Update()
        {
            if(vehicleController.Active)
            {
                bool changeCamera = false;
                try
                {
                    changeCamera = Input.GetButtonDown("ChangeCamera");
                }
                catch
                {
                    Debug.LogWarning("ChangeCamera key not set inside input manager. Falling back to default.");
                    changeCamera = Input.GetKeyDown(KeyCode.C);
                }

                if (changeCamera)
                {
                    NextCamera();
                    CheckIfInside();
                }

                if (previousCamera != currentCamera || vehicleController.Active && !wasActive)
                {
                    DisableAllCamerasExceptCurrent();
                    CheckIfInside();
                }

                previousCamera = currentCamera;
            }
            else
            {
                DisableAllCameras();
            }

            wasActive = vehicleController.Active;
        }

        void CheckIfInside()
        {
            cis = vehicleCameras[currentCamera].gameObject.GetComponent<CameraInsideVehicle>();

            if (cis != null)
            {
                vehicleController.sound.insideVehicle = true;
            }
            else
            {
                vehicleController.sound.insideVehicle = false;
            }
        }

        /// <summary>
        /// Activates next camera in order the camera scripts are attached to the camera object.
        /// </summary>
        public void NextCamera()
        {
            if(vehicleCameras.Count > 0)
            {
                currentCamera++;
                if (currentCamera >= vehicleCameras.Count)
                    currentCamera = 0;
            }
        }

        void DisableAllCamerasExceptCurrent()
        {
            for (int i = 0; i < vehicleCameras.Count; i++)
            {
                if (i != currentCamera)
                {
                    vehicleCameras[i].SetActive(false);
                    AudioListener al = vehicleCameras[i].GetComponent<AudioListener>();
                    if (al != null)
                    {
                        al.enabled = false;
                    }
                }
                else
                {
                    vehicleCameras[i].SetActive(true);
                    AudioListener al = vehicleCameras[i].GetComponent<AudioListener>();
                    if(al != null)
                    {
                        al.enabled = true;
                    }
                }
            }
        }

        void DisableAllCameras()
        {
            for (int i = 0; i < vehicleCameras.Count; i++)
            {
                vehicleCameras[i].SetActive(false);
            }
        }
    }
}

