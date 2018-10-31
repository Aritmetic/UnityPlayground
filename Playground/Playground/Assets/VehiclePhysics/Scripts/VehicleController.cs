using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VehicleController : MonoBehaviour {

    public new Rigidbody rigidbody;
    public bool customCenterOfMass = false;
    public Vector3 centerOfMass;

    public Steering steering;

    public Engine engine;

    public Transmission transmission;

    [Tooltip("SI Unit KW")]
    public float MaxTorque = 400f;
    public float MaxSteerAngle = 40.0f;

    public Vector3 frontDownForcePoint = Vector3.zero;
    public float frontValue = 1.0f;
    public Vector3 backDownForcePoint = Vector3.zero;
    public float backValue = 1.0f;

    public Axle[] axles;
    //public WheelColliderVP[] colls;

    [Header("Debug")]
    public Text debugText;

	// Use this for initialization
	void Start () {
        rigidbody.centerOfMass = centerOfMass;

        foreach (Axle a in axles)
            a.setRigidbody(rigidbody);
        
	}
	
    [System.Serializable]
    public class Spoiler
    {
        // https://en.wikipedia.org/wiki/Downforce
        // http://www.ppl-flight-training.com/lift-formula.html

        public Transform transform;

        [Tooltip("Co-Effiecient of Lift")]
        public float CL = 2.0f;
        [Tooltip("Surface Area")]
        public float s = 2f;

        public float W = 2.0f;
        public float H = .3f;
        public float F = .3f;
        public float multiplicator = 10f;

        public const float p = 1.255f;
        public const float pHalf = .62775f;
        
        public float GetDownForce(float v)
        {
            return CL * pHalf * v * s * multiplicator;
            //return .5f * W * H * F * p * v;
        }
    }

    public Spoiler[] downForces;

	// Update is called once per frame
	void FixedUpdate() {
        rigidbody.centerOfMass = centerOfMass;

        float sqaureVelocity = rigidbody.velocity.magnitude * rigidbody.velocity.magnitude;
        
        float steerInput = Input.GetAxis("Horizontal");
        float motorTorque = Input.GetAxis("Vertical") * MaxTorque;

        string t = "";

        for (int i = 0; i < downForces.Length; ++i)
        {
            rigidbody.AddForceAtPosition(-transform.up * downForces[i].GetDownForce(sqaureVelocity), downForces[i].transform.position);
        }

        for (int i = 0; i < axles.Length; ++i)
        {
            Axle axle = axles[i];

            axle.steerInput = steerInput;
            axle.motorTorque = motorTorque;
            axle.FixedUpdate();
            
            t += axle.ToString() + "\n";
        }
        t += "v: " + rigidbody.velocity.magnitude * 3.6f + "km/h";

        if (debugText != null)
        {
            debugText.text = t;
        }

	}

    //[ContextMenu("Init")]
    //public void Initialize()
    //{
    //    if (rigidbody == null) gameObject.AddComponent<Rigidbody>();

    //    rigidbody = GetComponent<Rigidbody>();
    //    if (customCenterOfMass)
    //        rigidbody.centerOfMass = centerOfMass;
    //    else
    //        centerOfMass = rigidbody.centerOfMass;
    //}
}
