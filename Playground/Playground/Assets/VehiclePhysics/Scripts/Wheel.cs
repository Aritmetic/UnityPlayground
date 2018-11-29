using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



[System.Serializable]
public class Wheel
{
    public string name;

    public float mass = 25.0f;
    public float tireRadius = 0.5f;
    public float width = 0.2f;

    [HideInInspector]
    public float steerAngle = 0.0f;


    [HideInInspector]
    public float motorTorque = 0.0f;

    [HideInInspector]
    public float RPM
    {
        get
        {
            return (forwardSpeed * 60f )/TireCircumfence;
        }
    }
    [HideInInspector]
    public float TireCircumfence
    {
        get { return tireRadius * Mathf.PI;  }
    }

    [HideInInspector]
    public Vector3 previousPosition = Vector3.zero;

    // INFO
    [HideInInspector]
    public float acceleration = 0.0f;

    [HideInInspector]
    public Vector3 totalSpeed = Vector3.zero;
    [HideInInspector]
    public Vector3 previousSpeed = Vector3.zero;

    [HideInInspector]
    public float forwardSpeed = 0.0f;
    [HideInInspector]
    public float sidewaySpeed = 0.0f;

    [HideInInspector]
    public float forwardForce = 0.0f;
    [HideInInspector]
    public float sidewayForce = 0.0f;

    //**********
    // Wheel Hit
    //**********
    [HideInInspector]
    public RaycastHit hit;
    [HideInInspector]
    public bool IsGrounded = false;
    [HideInInspector]
    public bool isSpinning = false;

    [HideInInspector]
    public string frictionName = "Default";
    [HideInInspector]
    public Friction friction
    {
        get
        {
            return Friction.Instance.list[frictionName];
        }
    }
    public float forwardMy
    {
        get
        {
            if (totalSpeed.magnitude <= 1.0f) MonoBehaviour.print("Static");
            else if (isSpinning) MonoBehaviour.print("Spinning");
            else MonoBehaviour.print("Rolling");

            if (totalSpeed.magnitude <= 1.0f) return friction.forward.Static;
            else if (isSpinning) return friction.forward.Dynamic;
            return friction.forward.Rolling;
        }
    }
    public float sidewayMy
    {
        get
        {
            if (totalSpeed.magnitude <= 1.0f) return friction.sideway.Static;
            else if (isSpinning) return friction.sideway.Dynamic;
            return friction.sideway.Rolling;
        }
    }

    //**********
    //Visuals
    //**********
    public GameObject Visual;

    public Wheel()
    {
        this.mass = 25.0f;
        this.tireRadius = 0.5f;
        this.width = 0.2f;
    }

    // Debug
    public override string ToString()
    {
        return name + ": a: " + acceleration + " totalSpeed: " + totalSpeed.magnitude;
    }

}