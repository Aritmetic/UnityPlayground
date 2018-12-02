using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Axle {

    public string Name;

    [Space(5)]

    [Header("Steering")]
    [Range(0.0f, 90.0f)] public float steerAngleMax = 0.0f;

    [Header("Engine")]
    [Range(0.0f, 1.0f)] public float torqueCoefficient = 1.0f;
    //[Range(0.0f, 1.0f)] public float engineBrakeCoefficient = 0.0f;

    [Header("Braking")]
    [Range(0.0f, 1.0f)] public float brakeCoefficient = 1.0f;
    [Range(0.0f, 1.0f)] public float handbrakeCoefficient = 1.0f;




    [Space(5)]

    public float GearRatio = 3f;
    
    [Space(5), Header("Wheels")]

    
    [HideInInspector]
    public float motorTorque = 0.0f;
    [HideInInspector]
    public float steerInput = 0.0f;
    
    public void FixedUpdate()
    {
        //leftWheel.wheel.motorTorque = (motorTorque/2) * torqueCoefficient;
        //rightWheel.wheel.motorTorque = (motorTorque/2) * torqueCoefficient;

        //leftWheel.wheel.steerAngle = getAckermannAngle(leftWheel.WheelObject.transform.localPosition.x, steerInput * steerAngleMax);
        //rightWheel.wheel.steerAngle = getAckermannAngle(rightWheel.WheelObject.transform.localPosition.x, steerInput * steerAngleMax);
        
        //leftWheel.FixedUpdate();
        //rightWheel.FixedUpdate();

    }

    private float getAckermannAngle(float length, float steerAngle)
    {
        /*if (steerAngle.Equals(0f)) return 0f;
        
        float alpha = 90 - Mathf.Abs(steerAngle);

        float b = (geometry.ackermann * Mathf.Sin(alpha * Mathf.Deg2Rad)) / Mathf.Sin(steerAngle * Mathf.Deg2Rad);

        return Mathf.Atan( (geometry.ackermann / ( b - length ))) * Mathf.Rad2Deg;
   */
        return 0f; }

    // used as init
    public void setRigidbody(Rigidbody rb)
    {
        //leftWheel.rigidbody = rb;
        //rightWheel.rigidbody = rb;
    }

}
