using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Engine {

    [Tooltip("Max Torque Engine can produce.")]
    public float maxTorque = 250f; // si-unit kw;

    [Tooltip("v in km/h. 0 when no  max velocity.")]
    public float maxVelocity = 0f;

    [Range(0, 5000)]  public int minRPM = 550;
    [Range(0, 25000)] public int maxRPM = 9000;
    public float currentRpm;

    public AnimationCurve[] curves; 

}
