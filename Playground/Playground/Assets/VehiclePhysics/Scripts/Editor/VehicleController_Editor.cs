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
        Handles.SphereHandleCap(0, vehicle.transform.position + vehicle.centerOfMass, Quaternion.identity, .2f, EventType.Repaint);
    }
}
