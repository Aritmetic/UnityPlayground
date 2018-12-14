using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



[System.Serializable]
public class Suspension
{

    public float SpringForce = 25000.0f;
    public float Damper = 3500.0f;
    public float Distance = 0.3f;
    [Range(0.0f, 1.0f)] public float targetPosition = 0.5f;

    public bool debug = false;

    [HideInInspector]
    private float compression = 0.0f;
    public float Compression
    {
        get { return compression; }
        set { compression = value; }
    }
    [HideInInspector]
    public float PreviousCompression = 0.0f;

    [HideInInspector]
    public float Force = 0.0f;
    [HideInInspector]
    public float DampingForce = 0.0f;

    public float TotalForce
    {
        get
        {
            return Force + DampingForce;
        }
    }

    public Suspension()
    {
        this.SpringForce = 25000.0f;
        this.Damper = 3500.0f;
        this.Distance = 0.3f;
        this.targetPosition = 0.5f;
    }
}