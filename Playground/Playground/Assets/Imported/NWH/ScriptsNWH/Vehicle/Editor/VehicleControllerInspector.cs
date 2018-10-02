using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using NWH.WheelController3D;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Shows different settings for trailer depending if vehicle is a trailer or a towing vehicle.
    /// </summary>
    [CustomEditor(typeof(VehicleController))]
    [CanEditMultipleObjects]
    [ExecuteInEditMode]
    public class VehicleControllerInspector : Editor
    {
        private bool showTrailerSettings = false;
        private bool showGeneralSettings = false;

        private static Texture2D staticRectTexture;
        private static GUIStyle staticRectStyle;
        private Texture2D logo;
        private Rect lastRect;

        private void Awake()
        {
            staticRectTexture = new Texture2D(1, 1);
            staticRectStyle = new GUIStyle();
            logo = Resources.Load("Publishing/Logo") as Texture2D;
        }

        public override void OnInspectorGUI()
        {
            VehicleController vehicleController = (VehicleController)target;

            GUILayout.Space(40);
            lastRect = GUILayoutUtility.GetLastRect();

            DrawDefaultInspector();
            TrailerSettings();
            GeneralSettings();

            if(!vehicleController.GetComponent<DesktopInputManager>() && !vehicleController.GetComponent<MobileInputManager>())
            {
                EditorGUILayout.HelpBox("Using input from vehicle manager or other external source. Check 'Input' section inside manual for other options.", MessageType.Info, true);
            }

            if (Time.fixedDeltaTime > 0.017f)
            {
                EditorGUILayout.HelpBox("Fixed Delta Time is " + Time.fixedDeltaTime + ". It is recommended to use 0.017 or lower.", MessageType.Warning, true);
            }

            if (logo != null)
            {
                float width = Screen.width;
                float height = 53f;
                float logoHeight = height * 0.7f;
                float logoWidth = logoHeight * 8.34f;

                GUIDrawRect(new Rect(0, lastRect.y + 3, width, height), new Color(0.89f, 0.89f, 0.89f));
                GUI.DrawTexture(new Rect(lastRect.x + width * 0.5f - logoWidth * 0.55f, lastRect.y + height * 0.5f - logoHeight * 0.4f, logoWidth, logoHeight), logo);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(vehicleController);
        }

        private void TrailerSettings()
        {
            showTrailerSettings = EditorGUILayout.Foldout(showTrailerSettings, "Trailer");
            if (showTrailerSettings)
            {
                EditorGUI.indentLevel++;
                SerializedProperty trailerIsTrailer = serializedObject.FindProperty("trailer.isTrailer");
                EditorGUILayout.PropertyField(trailerIsTrailer, new GUIContent("Is Trailer", "Check if vehicle is trailer and not a truck."));
                EditorGUILayout.ObjectField(serializedObject.FindProperty("trailer.attachmentPoint"), new GUIContent("Attachment Point", 
                    "Point at which trailer will be attached represented by an (empty) game object."));

                // Trailer
                if (trailerIsTrailer.boolValue)
                {
                    EditorGUILayout.ObjectField(serializedObject.FindProperty("trailer.trailerStand"), new GUIContent("Trailer Stand", 
                        "Trailer stand object that will appear when trailer is connected"));
                }
                // Truck
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trailer.trailersTag"), new GUIContent("Acceptable Trailer Tag", 
                        "Only this tag will be taken into consideration when looking for a trailer"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trailer.attachDistanceThreshold"), new GUIContent("Attach Distance Threshold", 
                        "Distance trailer attachment point needs to be in from truck attachment " +
                        "point for trailer to be attachable"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trailer.attachOnPlay"), new GUIContent("Attach On Play", "If in range, trailer will be attached on Start"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trailer.breakForce"), new GUIContent("Joint Break Force", "Force needed to disconnect the trailer."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trailer.maxNoTrailerPowerReduction"), new GUIContent("No Trailer Power Reduction", 
                        "Power reduction in percentage of engine power when there is no " +
                        "trailer attached. Use this to reduce wheel spin on powerful trucks when binary controller is used."));
                }
                EditorGUI.indentLevel--;
            }
        }


        private void GeneralSettings()
        {
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General");
            if(showGeneralSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("forwardSlipThreshold"), new GUIContent("Forward Slip Threshold", 
                    "If the forward slip of the wheel exceeds this value wheel spin will be registered. " +
                    "Lower value means skidmarks and other effects will play sooner and with higher intensity."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sideSlipThreshold"), new GUIContent("Side Slip Threshold", 
                    "If the side (lateral) slip of the wheel exceeds this value wheel skid will be registered."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speedLimiter"), new GUIContent("Speed Limiter", "Speed limit in m/s"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("freezeWhenStill"), new GUIContent("Freeze When Still", "Freezes the rigidbody when vehicle is still and active to prevent" +
                    " gradual creep."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("freezeWhenInactive"), new GUIContent("Freeze When Inactive", "Freezes the rigidbody when vehicle is inactive to prevent" +
                    " gradual creep."));
                EditorGUI.indentLevel--;
            }
        }

        void GUIDrawRect(Rect position, Color color)
        {

            if(staticRectTexture == null) staticRectTexture = new Texture2D(1, 1);
            staticRectTexture.SetPixel(0, 0, color);
            staticRectTexture.Apply();

            if (staticRectStyle == null) staticRectStyle = new GUIStyle();
            staticRectStyle.normal.background = staticRectTexture;

            GUI.Box(position, GUIContent.none, staticRectStyle);

        }
    }
}

