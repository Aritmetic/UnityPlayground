using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Engine {

    public float maxTorque = 1500f;

    public float minRPM = 500f;
    public float maxRPM = 500f;

    [Header("Gears")]
    [Range(0.0f, 20f)] public float[] forwardGears;
    [Range(0.0f, 20f)] public float[] reverseGears;

    

}
