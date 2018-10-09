using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelColliderVP : MonoBehaviour {

    [SerializeField]
    public Wheel wheel;

    [SerializeField]
    public Suspension suspension;

    [SerializeField]
    public Friction[] frictions;

    private new Rigidbody rigidbody;

    public bool showDebug = false;

	// Use this for initialization
	void Start () {
        rigidbody = GetComponentInParent<Rigidbody>();
        
        if (wheel.Visual == null)
        {
            wheel.Visual = transform.GetChild(0).gameObject;
        }
	}

    private void FixedUpdate()
    {

        UpdateSuspension();
        CalculateForces();
        ApplyForces();

        UpdateVisuals();

        wheel.MotorTorque = 0.0f;

        if (showDebug) DrawDebug(); 

    }


    public void UpdateSuspension()
    {

        Vector3 origin = transform.position;

        // Set default values;
        wheel.IsGrounded = false;
        suspension.PreviousCompression = suspension.Compression;
        suspension.Compression = 0.0f;

        if(Physics.Raycast(transform.position, -transform.up, out wheel.hit, suspension.Distance + wheel.tireRadius))
        {
            wheel.IsGrounded = true;
            suspension.Compression = suspension.Distance + wheel.tireRadius - (wheel.hit.point - transform.position).magnitude;
        }
        
    }

    public void CalculateForces()
    {
        suspension.Force = 0.0f;
        suspension.DampingForce = 0.0f;

        if (!wheel.IsGrounded) return;

        CalculateSuspensionForce();
        CalculateFriction();
        
    }

    public void CalculateSuspensionForce()
    {
        suspension.Force = (suspension.Compression - suspension.Distance * suspension.targetPosition) * suspension.SpringForce;
        
        suspension.DampingForce = (suspension.Compression - suspension.PreviousCompression) / Time.fixedDeltaTime * suspension.Damper;

    }

    public void CalculateFriction()
    {
        wheel.previousSpeed = wheel.totalSpeed;
        wheel.totalSpeed = 3.6f * ((transform.position - wheel.previousPosition) / Time.deltaTime);

        wheel.sidewaySpeed = (wheel.totalSpeed.x * wheel.Visual.transform.right.x + wheel.totalSpeed.y * wheel.Visual.transform.right.y + wheel.totalSpeed.z * wheel.Visual.transform.right.z);
        wheel.forwardSpeed = (wheel.totalSpeed.x * wheel.Visual.transform.forward.x + wheel.totalSpeed.y * wheel.Visual.transform.forward.y + wheel.totalSpeed.z * wheel.Visual.transform.forward.z);

        wheel.acceleration = (wheel.totalSpeed.magnitude - wheel.previousSpeed.magnitude) / Time.deltaTime;

        float NormalForce = (rigidbody.mass * Physics.gravity.y) / 4; //# todo suspension force need to be applied. Also DonwFOrce from wind.
        

        // Sideway Friction
        float sidewayFrictionMax = .5f * rigidbody.mass * (wheel.sidewaySpeed * wheel.sidewaySpeed);

        float sidewayFriction = Mathf.Abs(NormalForce * frictions[0].lateralStaticMy); // Max

        sidewayFriction = Mathf.Clamp(sidewayFriction, 0, Mathf.Abs(sidewayFrictionMax));

        wheel.sidewayForce = -Mathf.Sign(wheel.sidewaySpeed) * sidewayFriction;


        // Forward Friction
        float forwardFrictionMax = .5f * rigidbody.mass * (wheel.forwardSpeed * wheel.sidewaySpeed);

        float forwardFriction = Mathf.Abs(NormalForce * frictions[0].staticMy);
        
        forwardFriction = Mathf.Clamp(forwardFriction, 0, Mathf.Abs(forwardFrictionMax));
        
        wheel.forwardForce = wheel.MotorTorque + -Mathf.Sign(wheel.forwardSpeed) * forwardFriction;
        
        wheel.previousPosition = transform.position;

        if (!showDebug) return;
        Debug.DrawLine(wheel.hit.point, wheel.hit.point + forwardFriction * wheel.Visual.transform.forward, Color.green);
    }

    public void ApplyForces()
    {

        // Suspension
        rigidbody.AddForceAtPosition(wheel.hit.normal * suspension.TotalForce, transform.position);

        // Friction
        rigidbody.AddForceAtPosition(wheel.sidewayForce * wheel.Visual.transform.right, wheel.hit.point);
        rigidbody.AddForceAtPosition(wheel.forwardForce * wheel.Visual.transform.forward, wheel.hit.point);
        
    }

    public void UpdateVisuals()
    {
        Vector3 p;
        Quaternion q;

        GetWorldPos(out p, out q);

        wheel.Visual.transform.localPosition = p;
        wheel.Visual.transform.localRotation = q;
    }

    public void GetWorldPos(out Vector3 position, out Quaternion rotation)
    {
        position = new Vector3(0, -suspension.Distance + suspension.Compression, 0);
        Quaternion q = Quaternion.identity;

        q.y += Quaternion.Euler(0, wheel.SteerAngle, 0).y;

        rotation = q;
    }

    public void DrawDebug()
    {
        if (!showDebug) return;
        // CurrentSuspension.
        Debug.DrawLine(
            transform.position, 
            transform.position - new Vector3(0, suspension.Distance - suspension.Compression + wheel.tireRadius, 0), 
            Color.green);

        // Speed Vectors
        Vector3 origin = wheel.hit.point;
        Debug.DrawLine(origin, origin + wheel.totalSpeed, Color.red);
        Debug.DrawLine(origin, origin + wheel.Visual.transform.right * wheel.sidewaySpeed, Color.blue);
        Debug.DrawLine(origin, origin + wheel.Visual.transform.forward * wheel.forwardSpeed, Color.yellow);
    }

    #region Classes
    [System.Serializable]
    public class Wheel
    {

        public string name;

        public float mass = 25.0f;
        public float tireRadius = 0.5f;
        public float width = 0.2f;

        public bool isSteerable = false;

        private float steerAngle = 0.0f;
        public float SteerAngle
        {
            get { return (isSteerable) ? steerAngle : 0.0f;}
            set { steerAngle = value; }
        }

        [HideInInspector]
        public float MotorTorque = 0.0f;
        
        public float rpm = 0.0f;

        [HideInInspector]
        public Vector3 previousPosition = Vector3.zero;

        public float acceleration = 0.0f;

        public Vector3 totalSpeed = Vector3.zero;
        public Vector3 previousSpeed = Vector3.zero;

        public float forwardSpeed = 0.0f;
        public float sidewaySpeed = 0.0f;

        public float forwardForce = 0.0f;
        public float sidewayForce = 0.0f;


        [HideInInspector]
        public RaycastHit hit;
        public bool IsGrounded = false;

        public GameObject Visual;

        public override string ToString()
        {
            return "a: " + acceleration;
        }

    }

    [System.Serializable]
    public class Suspension
    {

        public float SpringForce = 25000.0f;
        public float Damper = 3500.0f;
        public float Distance = 0.3f;
        [Range(0.0f, 1.0f)] public float targetPosition = 0.5f;

        public float Compression = 0.0f;
        public float PreviousCompression = 0.0f;

        public float Force = 0.0f;
        public float DampingForce = 0.0f;

        public float TotalForce
        {
            get { return Force + DampingForce; }
        }
    }

    [System.Serializable]
    public class Friction
    {

        public enum Enums
        {
            first, second
        }

        public string name = "DefaultFriction";

        // Forward
        public float staticMy = 0.8f;
        public float rollingMy = 0.3f;
        public float dynamicMy = 0.7f;

        // Sideways
        public float lateralStaticMy = 0.3f;
        public float lateralRollingMy = 0.1f;
        public float lateralDynamicMy = 0.2f;


        public static Friction[] list;

        public Friction()
        {
            staticMy = .8f;
        }

    }

    #endregion

}
