using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VehicleController))]
public class VehicleController_Editor : Editor {

    VehicleController vehicle;
    
    private void OnEnable()
    {
        vehicle = target as VehicleController;

    }

    private void OnSceneGUI()
    {
        Handles.color = Color.red;
        Vector3 pos = vehicle.transform.rotation * vehicle.centerOfMass;
        Handles.SphereHandleCap(0, vehicle.transform.position + pos, Quaternion.identity, .2f, EventType.Repaint);
    }
}
