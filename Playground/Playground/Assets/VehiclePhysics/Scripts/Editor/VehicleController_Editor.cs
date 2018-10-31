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

        DrawCollider();
    }

    private void DrawCollider()
    {
        foreach(Axle axle in vehicle.axles)
        {
            drawColl(axle.leftWheel);
            drawColl(axle.rightWheel);

            Handles.color = Color.green;
            Handles.DrawLine(
                axle.leftWheel.WheelObject.transform.position,
                axle.rightWheel.WheelObject.transform.position
                );
        }
    }

    private void drawColl(WheelColliderVP coll)
    {
        Vector3 origin = coll.WheelObject.transform.position;
        Vector3 up = coll.WheelObject.transform.up;
        Vector3 suspensionTarget = origin - coll.WheelObject.transform.up * (coll.suspension.Distance - coll.suspension.Compression);
        

        // Draw Start
        Handles.color = new Color(1f, .7f, 0f);
        drawHorizontalLine(origin, .15f);

        // Draw Suspension Spring
       Handles.color = Color.red;
        Handles.DrawLine(
            coll.WheelObject.transform.position,
            suspensionTarget
        );

        // Draw Tire Radius
        Handles.color = Color.green;
        Handles.DrawLine(
            suspensionTarget,
            suspensionTarget - up * (coll.wheel.tireRadius)
            );

        // Draw Target Position
        Handles.color = new Color(1f, .7f, 0f);
        drawHorizontalLine(origin - up * (coll.suspension.Distance * coll.suspension.targetPosition), .1f);

        // Draw Wheel Collider
        Handles.color = Color.green;
        Handles.DrawWireArc(
            suspensionTarget + coll.WheelObject.transform.right * coll.wheel.width / 2,
            coll.WheelObject.transform.right,
            Vector3.up,
            360.0f,
            coll.wheel.tireRadius
        );
        Handles.DrawWireArc(
            suspensionTarget + coll.WheelObject.transform.right * -coll.wheel.width / 2,
            coll.WheelObject.transform.right,
            Vector3.up,
            360.0f,
            coll.wheel.tireRadius
        );

        // Draw End
        Handles.color = new Color(1f, .7f, 0f);
        drawHorizontalLine(suspensionTarget, .15f);

        if (coll.WheelObject.transform.GetChild(0) != null)
            coll.wheel.Visual = coll.WheelObject.transform.GetChild(0).gameObject;

        if (coll.wheel.Visual != null)
            coll.wheel.Visual.transform.position = suspensionTarget;
    }

    private void drawHorizontalLine(Vector3 pos, float width)
    {
        Handles.DrawLine(
            pos + new Vector3(0f, 0f, -width/2),
            pos + new Vector3(0f, 0f, width / 2)
            );
        Handles.DrawLine(
            pos + new Vector3(-width / 2, 0f, 0f),
            pos + new Vector3(width / 2, 0f, 0f)
            );
    }
}
