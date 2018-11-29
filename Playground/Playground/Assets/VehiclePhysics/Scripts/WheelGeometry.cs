using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WheelGeometry
{

    [Tooltip("The turning point. Mostly the rear axle.")] public float ackermann;
    [Range(-15f, 15f)] public float toeAngle = 0f;

    public float camber;

}