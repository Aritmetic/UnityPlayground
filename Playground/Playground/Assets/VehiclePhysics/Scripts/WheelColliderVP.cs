using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelColliderVP : MonoBehaviour {

    [SerializeField]
    public Wheel wheel;

    [SerializeField]
    public Suspension suspension;

    [SerializeField]
    public Friction[] frictionList;

    private Rigidbody rigidbody;

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
        
    }

    public void CalculateSuspensionForce()
    {
        suspension.Force = (suspension.Compression - suspension.Distance * suspension.targetPosition) * suspension.SpringForce;
        
        suspension.DampingForce = (suspension.Compression - suspension.PreviousCompression) / Time.fixedDeltaTime * suspension.Damper;

    }

    public void ApplyForces()
    {

        // Suspension
        rigidbody.AddForceAtPosition(Vector3.up * suspension.TotalForce, transform.position);

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
        Quaternion q = transform.rotation;

        q.y += Quaternion.Euler(0, wheel.SteerAngle, 0).y;

        rotation = q;
    }

    public void DrawDebug()
    {
        // CurrentSuspension.
        Debug.DrawLine(
            transform.position, 
            transform.position - new Vector3(0, suspension.Distance - suspension.Compression + wheel.tireRadius, 0), 
            Color.green);
    }

    #region Classes
    [System.Serializable]
    public class Wheel
    {

        public float mass = 25.0f;
        public float tireRadius = 0.5f;
        public float width = 0.2f;

        [HideInInspector] public float SteerAngle = 0.0f;
        [HideInInspector] public float MotorTorque = 0.0f;
        
        public float rpm = 0;

        [HideInInspector]
        public RaycastHit hit;
        public bool IsGrounded = false;

        public GameObject Visual;

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
        
    }

    #endregion

}
