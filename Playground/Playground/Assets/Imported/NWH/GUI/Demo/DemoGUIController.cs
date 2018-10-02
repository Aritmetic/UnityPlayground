using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Written only for demo purposes.
    /// Messy code ahead - you have been warned!
    /// </summary>
    public class DemoGUIController : MonoBehaviour
    {
        public VehicleChanger vehicleChanger;

        public Text promptText;
        public CharacterVehicleChanger characterVehicleChanger;

        public GameObject helpWindow;
        public GameObject settingsWindow;
        public GameObject telemetryWindow;

        void Update()
        {
            if(vehicleChanger.ActiveVehicleController != null && vehicleChanger.ActiveVehicleController.trailer.trailerInRange && !vehicleChanger.ActiveVehicleController.trailer.attached)
            {
                promptText.text = "Press T to attach trailer.";
            }
            else if(characterVehicleChanger != null && characterVehicleChanger.nearVehicle)
            {
                promptText.text = "Press V to enter vehicle.";
            }
            else if(vehicleChanger.ActiveVehicleController != null && vehicleChanger.ActiveVehicleController.flipOver.manual && vehicleChanger.ActiveVehicleController.flipOver.flippedOver)
            {
                promptText.text = "Press P to recover the vehicle.";
            }
            else
            {
                promptText.text = "";
            }
        }

        public void ToggleHelpWindow()
        {
            helpWindow.SetActive(!helpWindow.activeInHierarchy);
            settingsWindow.SetActive(false);
            telemetryWindow.SetActive(false);
        }

        public void ToggleSettingsWindow()
        {
            settingsWindow.SetActive(!settingsWindow.activeInHierarchy);
            helpWindow.SetActive(false);
            telemetryWindow.SetActive(false);
        }

        public void ToggleTelemetryWindow()
        {
            telemetryWindow.SetActive(!telemetryWindow.activeInHierarchy);
            settingsWindow.SetActive(false);
            helpWindow.SetActive(false);
        }

        public void ResetScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

