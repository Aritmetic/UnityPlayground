//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;

//[CustomEditor(typeof(WheelColliderVP))]
//[CanEditMultipleObjects]
//public class WheelColliderVP_Editor : Editor
//{

//    WheelColliderVP coll;

    

//    private void OnSceneGUI()
//    {
//        if( !(target is WheelColliderVP) ) return;

//        //coll = target as WheelColliderVP;
//        Handles.color = Color.red;
//        Handles.DrawLine(
//            coll.transform.position,
//            coll.transform.position - coll.transform.up * (coll.suspension.Distance - coll.suspension.Compression)
//        );

//        Handles.color = Color.green;
//        Handles.DrawWireArc(
//            coll.transform.position - coll.transform.up * (coll.suspension.Distance - coll.suspension.Compression),
//            coll.transform.right,
//            Vector3.up,
//            360.0f,
//            coll.wheel.tireRadius
//        );

//        if (coll.transform.GetChild(0) != null)
//            coll.wheel.Visual = coll.transform.GetChild(0).gameObject;

//        if (coll.wheel.Visual != null)
//            coll.wheel.Visual.transform.position = coll.transform.position - coll.transform.up * (coll.suspension.Distance - coll.suspension.Compression);

//    }
//}