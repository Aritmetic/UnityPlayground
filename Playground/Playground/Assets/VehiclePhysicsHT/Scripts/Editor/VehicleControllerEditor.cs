using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VehiclePhysics {

    [CustomEditor(typeof(VehicleController))]
    public class VehicleControllerEditor : Editor {

        VehicleController vc;

        private void OnEnable()
        {
            vc = target as VehicleController;
        }

        private void OnSceneGUI()
        {

            foreach(AxleData ad in vc.axles)
            {
                s(ad.leftWheel);
                s(ad.rightWheel);
            }

            DrawWheelCollider();
        }

        private void s(WheelData d)
        {
            if (!d.isGrounded) return;

            Handles.DrawSphere(0,
                d.hit.point,
                Quaternion.identity,
                .2f);
        }

        private void DrawWheelCollider()
        {
            foreach(AxleData axle in vc.axles)
            {
                DrawCollider(axle.leftWheel);
                DrawCollider(axle.rightWheel);
            }
        }

        private void DrawCollider(WheelData coll)
        {

            if (coll.visual == null) {

                Transform visual = coll.transform.GetChild(0);

                if (visual != null)
                {
                    coll.visual = visual;
                    Debug.LogWarning(coll.gameObject.name + " Wheels where automatically set.");
                } else
                {
                    Debug.LogError(coll.gameObject.name + ": Visual not set for Wheel. Can't draw Collider.");
                    return;
                }
            }

            Vector3 origin = coll.transform.position;
            Vector3 suspensionTarget = origin - coll.transform.up * coll.suspension.Distance;
            Vector3 suspensionCompressed = suspensionTarget + coll.transform.up * coll.suspension.Compression;

            // Draw Start
            Handles.color = new Color(1f, .7f, 0f);
            drawHorizontalLine(origin, .15f);

            // Draw Suspesion Spring
            Handles.color = Color.red;
            Handles.DrawLine(
                origin, 
                suspensionTarget
                );

            // Draw Suspension compression.
            Handles.color = Color.blue;
            Handles.DrawLine(
                origin,
                suspensionCompressed
                );
            
            // Draw Collider
            Handles.color = Color.green;
            Handles.DrawWireArc(
                suspensionCompressed + coll.visual.transform.right * coll.width / 2,
                coll.visual.right,
                Vector3.up,
                360.0f,
                coll.tireRadius
                );
            Handles.DrawWireArc(
                suspensionCompressed + -coll.visual.transform.right * coll.width / 2,
                coll.visual.right,
                Vector3.up,
                360.0f,
                coll.tireRadius
                );


            // Draw End
            Handles.color = new Color(1f, .7f, 0f);
            drawHorizontalLine(suspensionTarget, .15f);
        }

        private void drawHorizontalLine(Vector3 pos, float width)
        {
            Handles.DrawLine(
                pos + new Vector3(0f, 0f, -width / 2),
                pos + new Vector3(0f, 0f, width / 2)
                );
            Handles.DrawLine(
                pos + new Vector3(-width / 2, 0f, 0f),
                pos + new Vector3(width / 2, 0f, 0f)
                );
        }

    }
}