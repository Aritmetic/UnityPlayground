using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Axle {

    public string Name;

    [Range(0.0f, 90.0f)] public float steerAngleMax = 0.0f;
    [Range(0.0f, 1.0f)] public float torqueCoefficient = 1.0f;
    [Range(0.0f, 1.0f)] public float brakeCoefficient = 1.0f;
    [Range(0.0f, 1.0f)] public float handbrakeCoefficient = 1.0f;


    public WheelColliderVP leftWheel;
    public WheelColliderVP rightWheel;
    
    [HideInInspector]
    public float motorTorque = 0.0f;
    [HideInInspector]
    public float steerInput = 0.0f;
    
    public void FixedUpdate()
    {
        leftWheel.wheel.motorTorque = (motorTorque/2) * torqueCoefficient;
        rightWheel.wheel.motorTorque = (motorTorque/2) * torqueCoefficient;

        leftWheel.wheel.steerAngle = steerInput * steerAngleMax;
        rightWheel.wheel.steerAngle = steerInput * steerAngleMax;


        leftWheel.FixedUpdate();
        rightWheel.FixedUpdate();

    }

    public void setRigidbody(Rigidbody rb)
    {
        leftWheel.rigidbody = rb;
        rightWheel.rigidbody = rb;
    }

}
