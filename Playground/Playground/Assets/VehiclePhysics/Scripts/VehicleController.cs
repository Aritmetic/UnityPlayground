using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VehicleController : MonoBehaviour {

    public Rigidbody rigidbody;
    public bool customCenterOfMass = false;
    public Vector3 centerOfMass;

    [Range(0.0f, 20000f)] public float testTorque = 0.0f;
    public float MaxTorque = 4000f;
    public float MaxSteerAngle = 40.0f;

    public Vector3 frontDownForcePoint = Vector3.zero;
    public float frontValue = 1.0f;
    public Vector3 backDownForcePoint = Vector3.zero;
    public float backValue = 1.0f;

    public WheelColliderVP[] colls;

    [Header("Debug")]
    public Text debugText;

	// Use this for initialization
	void Start () {
        Initialize();
	}
	
	// Update is called once per frame
	void Update () {
        rigidbody.centerOfMass = centerOfMass;

        float steerAngle = Input.GetAxis("Horizontal") * MaxSteerAngle;
        float motorTorque = Input.GetAxis("Vertical") * MaxTorque;

        string t = "";

        for(int i = 0; i < colls.Length; ++i)
        {
            WheelColliderVP coll = colls[i];

            coll.wheel.SteerAngle = steerAngle;
            coll.wheel.MotorTorque = motorTorque + testTorque;

            t += coll.wheel.ToString() + "\n";
        }
        t += "v: " + rigidbody.velocity.magnitude * 3.6f + "km/h";

        if (debugText != null)
        {
            debugText.text = t;
        }

	}

    [ContextMenu("Init")]
    public void Initialize()
    {
        if (rigidbody == null) gameObject.AddComponent<Rigidbody>();

        rigidbody = GetComponent<Rigidbody>();
        if (customCenterOfMass)
            rigidbody.centerOfMass = centerOfMass;
        else
            centerOfMass = rigidbody.centerOfMass;
    }
}
