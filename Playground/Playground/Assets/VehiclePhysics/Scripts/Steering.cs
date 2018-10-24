using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Steering {

    [Range(0.0f, 90f)]  public float maxSteerAngle = 30f;
    [Range(0.0f, 200f)] public float degreesPerSecond = 50f;
    
}
