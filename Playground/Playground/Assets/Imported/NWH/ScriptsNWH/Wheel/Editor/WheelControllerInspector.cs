using System.Linq;
using UnityEditor;
using UnityEngine;
using NWH.WheelController3D;

namespace NWH.WheelController3D
{
    [CustomEditor(typeof(WheelController))]
    [CanEditMultipleObjects]
    public class WheelControllerInspector : Editor
    {
        WheelController wc;

        public AnimationCurve forwardFrictionCurve;
        public AnimationCurve sideFrictionCurve;

        public Vector4 sideParams = new Vector4();
        public Vector4 forwardParams = new Vector4();

        // Wheel
        SerializedProperty tireRadius;
        SerializedProperty rimOffset;
        SerializedProperty wheelWidth;
        SerializedProperty wheelMass;
        SerializedProperty useRimCollider;
        SerializedProperty vehicleSide;

        // Geometry
        SerializedProperty camberCurve;

        // Spring
        SerializedProperty maxSpringForce;
        SerializedProperty springTravel;
        SerializedProperty springForceCurve;

        // Damper
        SerializedProperty damperBump;
        SerializedProperty damperRebound;
        SerializedProperty dampingForceCurve;

        // Forward friction 
        SerializedProperty fSlipCoefficient, fForceCoefficient;
        SerializedProperty fMaxForce;

        // Side friction
        SerializedProperty sSlipCoefficient, sForceCoefficient;
        SerializedProperty sMaxForce;

        // Scan
        SerializedProperty forwardScanResolution;
        SerializedProperty sideToSideScanResolution;

        // Misc
        SerializedProperty activeFrictionPresetEnum;
        SerializedProperty parentObject;
        SerializedProperty wheelVisual;
        SerializedProperty wheelNonRotating;
        SerializedProperty dbg;
        SerializedProperty scanIgnoreLayers;
        SerializedProperty singleRay;
        SerializedProperty applyForceToOthers;

        private bool hadParent = false;

        void OnEnable()
        {
            wc = (WheelController)target;

            // Wheel
            tireRadius = serializedObject.FindProperty("wheel.tireRadius");
            wheelWidth = serializedObject.FindProperty("wheel.width");
            rimOffset = serializedObject.FindProperty("wheel.rimOffset");
            wheelMass = serializedObject.FindProperty("wheel.mass");
            useRimCollider = serializedObject.FindProperty("useRimCollider");
            vehicleSide = serializedObject.FindProperty("vehicleSide");
            //rimRadius = serializedObject.FindProperty("wheel.rimRadius");
            //motorTorque = serializedObject.FindProperty("wheel.motorTorque");
            //brakeTorque = serializedObject.FindProperty("wheel.brakeTorque");

            // Geometry
            camberCurve = serializedObject.FindProperty("wheel.camberCurve");

            // Spring
            maxSpringForce = serializedObject.FindProperty("spring.maxForce");
            springTravel = serializedObject.FindProperty("spring.maxLength");
            springForceCurve = serializedObject.FindProperty("spring.forceCurve");

            // Damper
            damperBump = serializedObject.FindProperty("damper.unitBumpForce");
            damperRebound = serializedObject.FindProperty("damper.unitReboundForce");
            dampingForceCurve = serializedObject.FindProperty("damper.dampingCurve");


            // Forward friction
            fSlipCoefficient = serializedObject.FindProperty("fFriction.slipCoefficient");
            fForceCoefficient = serializedObject.FindProperty("fFriction.forceCoefficient");
            fMaxForce = serializedObject.FindProperty("fFriction.maxForce");

            // Side friction

            sSlipCoefficient = serializedObject.FindProperty("sFriction.slipCoefficient");
            sForceCoefficient = serializedObject.FindProperty("sFriction.forceCoefficient");
            sMaxForce = serializedObject.FindProperty("sFriction.maxForce");

            // Scan
            forwardScanResolution = serializedObject.FindProperty("forwardScanResolution");
            sideToSideScanResolution = serializedObject.FindProperty("sideToSideScanResolution");

            // Misc
            activeFrictionPresetEnum = serializedObject.FindProperty("activeFrictionPresetEnum");
            parentObject = serializedObject.FindProperty("parent");
            wheelVisual = serializedObject.FindProperty("wheel.visual");
            wheelNonRotating = serializedObject.FindProperty("wheel.nonRotating");
            dbg = serializedObject.FindProperty("debug");
            scanIgnoreLayers = serializedObject.FindProperty("scanIgnoreLayers");
            singleRay = serializedObject.FindProperty("singleRay");
            applyForceToOthers = serializedObject.FindProperty("applyForceToOthers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Set null fields to default values
            wc.Initialize();

            EditorGUI.BeginChangeCheck();

            // Check for parent and parent rigidbody
            Rigidbody rb = null;
            if (wc.Parent == null)
            {
                hadParent = false;
                EditorGUILayout.HelpBox("Parent object needs to be assigned (bottom of WC3D editor).", MessageType.Warning, true);
            }
            else
            {
                hadParent = true;
                if (!(rb = wc.Parent.GetComponent<Rigidbody>()))
                {
                    EditorGUILayout.HelpBox("There is no rigidbody attached to parent object.", MessageType.Warning, true);
                }
            }

            if (hadParent)
            {
                foreach (WheelController wc in targets) wc.VehicleSide = wc.VehicleSide;
            }


            //*************
            // Wheel
            //*************

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Wheel", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            if (wc.Parent)
            {
                EditorGUILayout.PropertyField(vehicleSide, new GUIContent("Side the wheel is on: "));
            }

            EditorGUILayout.PropertyField(tireRadius, new GUIContent("Tire Radius", "Radius of the whole wheel [m]."));
            EditorGUILayout.PropertyField(wheelWidth, new GUIContent("Wheel Width"));
            EditorGUILayout.PropertyField(wheelMass, new GUIContent("Wheel Mass"));
            EditorGUILayout.PropertyField(rimOffset, new GUIContent("Rim Offset", "Side offset of the rim. Positive value will result in wheel further from the vehicle."));

            GUILayout.Space(5);
            if (Application.isPlaying) GUI.enabled = false;
            EditorGUILayout.PropertyField(useRimCollider, new GUIContent("Use Rim Collider"));
            GUI.enabled = true;

            GUILayout.Space(5);

            //EditorGUILayout.PropertyField(motorTorque, new GUIContent("Motor Torque", "Driving torque at the center of the wheel, positive or negative [N]."));
            //EditorGUILayout.PropertyField(brakeTorque, new GUIContent("Brake Torque", "Stopping torque at the center of the wheel, positive only [N]."));

            EditorGUI.indentLevel--;


            //*************
            // Geometry
            //*************

            DrawHR();
            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(camberCurve, new GUIContent("Camber Curve:"), GUILayout.Height(60));
            EditorGUILayout.HelpBox("X: Spring compression [%], Y: Camber angle [deg]", MessageType.Info, true);

            EditorGUI.indentLevel--;


            //*************
            // Spring
            //*************

            DrawHR();
            EditorGUILayout.LabelField("Spring", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // Max force
            EditorGUILayout.PropertyField(maxSpringForce, new GUIContent("Max Spring Force", "Force applied when spring is fully compressed [N]."));
            if (rb != null && maxSpringForce.floatValue < rb.mass * -Physics.gravity.y)
            {
                EditorGUILayout.HelpBox("Spring force might be inadequate. Minimum recommended force for rigidbody mass of " + rb.mass + " is " + -(int)Physics.gravity.y * rb.mass, MessageType.Info, true);
            }

            // Spring travel
            EditorGUILayout.PropertyField(springTravel, new GUIContent("Spring Travel", "Distance from fully compressed spring to fully extended spring [m]. Very small values may bring unstable behavior."));


            // Spring curve
            EditorGUILayout.PropertyField(springForceCurve, new GUIContent("Spring Force Curve:"), GUILayout.Height(60));
            EditorGUILayout.HelpBox("X: Spring compression [%], Y: Force coefficient", MessageType.Info, true);

            EditorGUI.indentLevel--;


            //*************
            // Damper
            //*************

            DrawHR();
            EditorGUILayout.LabelField("Damper", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(damperBump, new GUIContent("Bump N/m/s"));
            EditorGUILayout.PropertyField(damperRebound, new GUIContent("Rebound N/m/s"));

            EditorGUILayout.PropertyField(dampingForceCurve, new GUIContent("Damping Curve:"), GUILayout.Height(60));
            EditorGUILayout.HelpBox("X: Spring velocity [m/s], Y: Force coefficient", MessageType.Info, true);

            EditorGUI.indentLevel--;


            //*************
            // Friction
            //*************

            DrawHR();
            EditorGUILayout.LabelField("Friction", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            GUILayout.Space(4);
            EditorGUILayout.PropertyField(activeFrictionPresetEnum, new GUIContent("Preset", "Surface friction preset."));
            foreach (WheelController w in targets)
            {
                w.activeFrictionPreset = WheelController.FrictionPreset.FrictionPresetList[(int)w.activeFrictionPresetEnum];
            }

            EditorGUILayout.LabelField("Current side friction curve:", EditorStyles.miniLabel);
            EditorGUILayout.CurveField(wc.activeFrictionPreset.Curve, GUILayout.Height(90));
            EditorGUILayout.LabelField("(note that preview is buggy in 5.3+, click on curve to see the real values!)", EditorStyles.miniLabel);

            EditorGUILayout.HelpBox("X: Slip, Y: Force coefficient", MessageType.Info, true);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Forward (longitudinal)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(fSlipCoefficient, new GUIContent("Slip Coefficient", "Modifier for the slip value. Default is 1."));
            EditorGUILayout.PropertyField(fForceCoefficient, new GUIContent("Force Coefficient", "Modifier for the force value. Default is 1."));
            EditorGUILayout.PropertyField(fMaxForce, new GUIContent("Max. Force", "Maximum force the tire can exert on the ground in the forward direction. Default is 0 = not limited."));

            GUILayout.Space(8);
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Side (lateral)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(sSlipCoefficient, new GUIContent("Slip Coefficient", "Modifier for the slip value. Default is 1."));
            EditorGUILayout.PropertyField(sForceCoefficient, new GUIContent("Force Coefficient", "Modifier for the force value. Default is 1."));
            EditorGUILayout.PropertyField(sMaxForce, new GUIContent("Max. Force", "Maximum force the tire can exert on the ground in the sideways direction. Default is 0 = not limited."));

            GUILayout.Space(5);
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            //*************
            // Wheel Scan
            //*************

            DrawHR();
            EditorGUILayout.LabelField("Wheel Scan", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(singleRay, new GUIContent("Single Ray Mode", "Only one raycast will be used, similar to what wheelcollider does. Can be activated at runtime."));
            if (Application.isPlaying) GUI.enabled = false;
            int suggestedForwardRes = (int)(Mathf.Clamp(wc.wheel.tireRadius * 20f, 5f, 30f));
            int suggestedSideRes = (int)(Mathf.Clamp(wc.wheel.width * 10f, 2f, 5f));
            EditorGUILayout.PropertyField(forwardScanResolution, new GUIContent("Forward Resolution (min. recommended: " + suggestedForwardRes + ")", "Scan resolution in the wheel's forward direction."));
            EditorGUILayout.PropertyField(sideToSideScanResolution, new GUIContent("Side Resolution (min. recommended: " + suggestedSideRes + ")", "Number of scan planes parallel to the wheel. Default is 3 (left, right and center of the wheel)"));
            if (forwardScanResolution.intValue < 5) forwardScanResolution.intValue = 5;
            if (sideToSideScanResolution.intValue < 1) sideToSideScanResolution.intValue = 1;
            GUI.enabled = true;
            EditorGUILayout.PropertyField(applyForceToOthers, new GUIContent("Apply Force To Hit Objects", "If enabled will apply force to the hit object."));

            int rayCount = wc.ForwardScanResolution * wc.SideToSideScanResolution;
            EditorGUILayout.HelpBox("Number of rays: " + rayCount, MessageType.Info, true);

            EditorGUI.indentLevel--;

            //*************
            // Misc
            //*************

            DrawHR();
            EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (Application.isPlaying) GUI.enabled = false;
            EditorGUILayout.PropertyField(parentObject, new GUIContent("Parent Object", "Vehicle's top object in the hierarchy."));
            EditorGUILayout.PropertyField(wheelVisual, new GUIContent("Wheel Visual", "Transform that is a visual representation of the wheel"));
            EditorGUILayout.PropertyField(wheelNonRotating, new GUIContent("Nonrotating Objects", "[Optional] Object that contains all of the nonrotating objects such as calipers and external fenders. " +
                "Object will follow the wheel at a fixed relative postiion but will not rotate with it."));
            EditorGUILayout.PropertyField(scanIgnoreLayers, new GUIContent("Scan Ignore Layers", "Only ticked layers will be detected."));
            GUI.enabled = true;
            EditorGUILayout.PropertyField(dbg, new GUIContent("Debug", "Show debug related gizmos."));


            GUILayout.Space(10);

            // Default layout
            // Uncomment next line to show the default inspector
            // base.OnInspectorGUI();

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(wc);
            }

            foreach (WheelController wc in targets)
            {
                wc.Initialize();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHR()
        {
            GUILayout.Space(4);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(4);
        }
    }
}
