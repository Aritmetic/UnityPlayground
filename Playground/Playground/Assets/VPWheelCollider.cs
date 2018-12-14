using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VPWheelCollider : MonoBehaviour {

    public float maxThrust;
    public float maxSteer;

    private float throttle;
    private float steer;


    public float fmu;
    public float lmu;

    public float tireRadius;
    public float springDistance;
    public float springForce;
    [Range(0f, 1f)] public float targetPose;
    public float damperForce;

    private float compression;
    private float prevCompression;

    private Rigidbody rigidbody;
    private RaycastHit hit;


    private Vector3 prevPos;

    private Quaternion r;

    [SerializeField]
    private Vector3 v;
    private Vector3 prevV;

    private void Start()
    {
        rigidbody = GetComponentInParent<Rigidbody>();
    }

    private void Update()
    {
        throttle = Input.GetAxis("Vertical");
        steer = Input.GetAxis("Horizontal");

        transform.localRotation = Quaternion.Euler(0f, maxSteer * steer, 0f);
    }

    private void FixedUpdate()
    {
        r = Quaternion.Inverse(transform.rotation);

        prevV = v;
        v = r * (transform.position - prevPos) / Time.fixedDeltaTime;

        prevPos = transform.position;

        prevCompression = compression;
        if(Physics.Raycast(transform.position, -transform.up, out hit, springDistance + tireRadius))
        {

            compression = springDistance + tireRadius - (hit.point - transform.position).magnitude;

            float fSpring = (compression - springDistance * targetPose) * springForce;
            float fDamper = ((compression - prevCompression) / Time.fixedDeltaTime) * damperForce;

            Vector3 suspensionForce = (fSpring + fDamper) * hit.normal;
            Vector3 sFR = Quaternion.Inverse(Quaternion.Euler(0f, transform.rotation.y, 0f)) * suspensionForce;

            float massOnSuspension = (fSpring + fDamper) / 9.81f;
            massOnSuspension = suspensionForce.y / 9.81f;

            //rigidbody.AddForceAtPosition(suspensionForce, hit.point);

            float fn = suspensionForce.y * lmu;
            float fnF = suspensionForce.y * fmu;

            float maxLateralFriction = ((v.x) / Time.fixedDeltaTime) * massOnSuspension;
            float maxForwardFriction = ((v.y) / Time.fixedDeltaTime) * massOnSuspension;

            float lateralForce = Mathf.Clamp(fn, 0f, Mathf.Abs(maxLateralFriction));
            float forwardForce = Mathf.Clamp(fnF, 0f, Mathf.Abs(maxForwardFriction));

            Vector3 force = -Mathf.Sign(v.x) * lateralForce * transform.right;
            force += suspensionForce;
            force += ((maxThrust ) * throttle) * transform.forward;
            force += -Mathf.Sign(v.y) * forwardForce * transform.forward; 

            rigidbody.AddForceAtPosition(force, hit.point);
            Debug.DrawLine(hit.point, hit.point + force);
            Debug.Log(v);
        }
    }
}

/*
 [Range(0f, 1f)] public float ff = 0.7f;
    [Range(0f, 1f)] public float fl = 0.7f;

    public float motorTorque;

    public float tireRadius;

    public float springForce;
    public float springDistance;
    public float damperForce;
    [Range(0f, 1f)] public float restPose;

    private RaycastHit hit;
    private Rigidbody rb;


	// Use this for initialization
	void Start () {
        rb = GetComponentInParent<Rigidbody>();
	}

    public float compression = 0f;
    public float prevCompression = 0f;

    public float maxS = 40f;
    private float s;

    private void Update()
    {
        
        s = Input.GetAxis("Horizontal");
        //if(transform.localPosition.z > 0)
        //transform.localRotation = Quaternion.Euler(0f, s * maxS, 0f);

        Quaternion r = Quaternion.Inverse( Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f) );

        prevPos = pos;
        pos = r * transform.position;

        preV = v;
        v = (pos - prevPos) / Time.deltaTime;

        prevA = a;
        a = (v - preV) / Time.deltaTime;
    }

    public Vector3 pos = Vector3.zero;
    public Vector3 prevPos;
    private float maxForce = 0f;

    public Vector3 v = Vector3.zero;
    private Vector3 preV;

    public Vector3 a = Vector3.zero;
    private Vector3 prevA;

  

    // Update is called once per frame
    void FixedUpdate () {

        

        prevCompression = compression;
        if (Physics.Raycast(transform.position, -transform.up, out hit, tireRadius + springDistance))
        {
            Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red);
            compression = springDistance + tireRadius - (hit.point - transform.position).magnitude;

            
            Debug.DrawLine(hit.point, hit.point + transform.up * (tireRadius + springDistance), Color.blue);
            Debug.DrawLine(hit.point, hit.point + transform.up * compression, Color.white);

            float fSpring = (compression - springDistance * restPose) * springForce;
            float fDamper = ((compression - prevCompression) / Time.fixedDeltaTime) * damperForce;

            Vector3 fSuspension = (fSpring + fDamper) * hit.normal;

            //Debug.DrawLine(hit.point, hit.point + fSuspension);

            susMass = fSuspension.y / 9.81f;

            rb.AddForceAtPosition(fSuspension, hit.normal);

            float lateralFriction = Mathf.Abs(fSuspension.y) * fl;
            float lateralFrictionMax = Mathf.Abs(susMass) * (v.x / Time.deltaTime);

            float max = Mathf.Abs(lateralFrictionMax);
            //max += Mathf.Abs(fSuspension.z);
            float appliedForce = -Mathf.Sign(v.x) * Mathf.Clamp(lateralFriction, 0f, max);
            //rb.AddForceAtPosition(appliedForce * transform.right, hit.point);
            Debug.Log(hit.transform.gameObject.name);
            //Debug.Log("appliedForce: " + appliedForce + " fSus: " + fSuspension + " max: " + max);
            /*
            
            float Fs = (compression - springDistance * restPose) * springForce;
            float Fd = ((compression - prevCompression) / Time.fixedDeltaTime) * damperForce;
            float FsT = Fs + Fd;

            susMass = FsT / 9.81f;

            Vector3 suspensionForce = FsT * hit.normal;
            
            rb.AddForceAtPosition(suspensionForce, hit.point);

            float lateralFriction = Mathf.Abs(FsT) * fl;
            float lateralFrictionMax = Mathf.Abs(susMass) * (v.x / Time.fixedDeltaTime) ;

            float appliedForce = -Mathf.Sign(v.x) * Mathf.Clamp(lateralFriction, 0f, Mathf.Abs(suspensionForce.z) + Mathf.Abs(lateralFrictionMax));
            Debug.Log("f: " + appliedForce + " sus: " + suspensionForce);
            rb.AddForceAtPosition(appliedForce * transform.right, hit.point);

            //Vector3 rot = Quaternion.Inverse(Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f)) * hit.normal;
            //Debug.Log(Vector2.Angle(transform.right, rot) );
            //float fr = FsT * fl * Mathf.Cos(Vector2.Angle(transform.right, rot) * Mathf.Deg2Rad);
            //float a2 = fr / (susMass);
            ////a2 = Mathf.Clamp(a2, -v.x, v.x);
            //float frForce = a2 * susMass * Time.fixedDeltaTime;
            //float j = Mathf.Clamp();
            //rb.AddForceAtPosition(-Mathf.Sign(v.x) * Mathf.Abs(frForce) * transform.right, hit.point);
            //rb.AddForceAtPosition(-Mathf.Sign(v.z) * Mathf.Abs(frForce) * transform.forward, hit.point);
            

        } else
        {
            compression = 0f;
        }

        motorTorque = 0f;
	}

    public float susMass;
     */
