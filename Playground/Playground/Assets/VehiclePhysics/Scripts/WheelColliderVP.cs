using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WheelColliderVP {

    [SerializeField]
    public GameObject WheelObject;

    [SerializeField]
    public Wheel wheel;

    [SerializeField]
    public Suspension suspension;

    [SerializeField]
    public Friction[] frictions;

    [HideInInspector]
    public Rigidbody rigidbody;

    public bool showDebug = false;
    public Slider suspensionSlider;
    
    public void Init()
    {
        if (WheelObject == null) return;

        if(WheelObject.transform.GetChild(0) != null)
        {
            wheel.Visual = WheelObject.transform.GetChild(0).gameObject;
        }
    }

    public void FixedUpdate()
    {

        UpdateSuspension();
        CalculateForces();
        ApplyForces();

        UpdateVisuals();

        wheel.motorTorque = 0.0f;

        DrawDebug(); 

    }


    public void UpdateSuspension()
    {

        Vector3 origin = WheelObject.transform.position;

        // Set default values;
        wheel.IsGrounded = false;
        suspension.PreviousCompression = suspension.Compression;
        suspension.Compression = 0.0f;

        if(Physics.Raycast(WheelObject.transform.position, -WheelObject.transform.up, out wheel.hit, suspension.Distance + wheel.tireRadius))
        {
            wheel.IsGrounded = true;
            suspension.Compression = suspension.Distance + wheel.tireRadius - (wheel.hit.point - WheelObject.transform.position).magnitude;
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
        wheel.totalSpeed = ((WheelObject.transform.position - wheel.previousPosition) / Time.deltaTime); //m/s

        // Split total force based on rotation.
        wheel.sidewaySpeed = (
              wheel.totalSpeed.x * wheel.Visual.transform.right.x 
            + wheel.totalSpeed.y * wheel.Visual.transform.right.y 
            + wheel.totalSpeed.z * wheel.Visual.transform.right.z);

        wheel.forwardSpeed = (
              wheel.totalSpeed.x * wheel.Visual.transform.forward.x 
            + wheel.totalSpeed.y * wheel.Visual.transform.forward.y 
            + wheel.totalSpeed.z * wheel.Visual.transform.forward.z);
        
        wheel.acceleration = (wheel.totalSpeed.magnitude - wheel.previousSpeed.magnitude) / Time.deltaTime;

        float NormalForce = suspension.TotalForce;//(rigidbody.mass * Physics.gravity.y)/4; //# todo suspension force need to be applied. Also DonwFOrce from wind.

        // Sideway Friction
        float sidewayFrictionMax = (.5f * rigidbody.mass * (wheel.totalSpeed.magnitude * wheel.totalSpeed.magnitude));
        
        float sidewayFriction = Mathf.Abs(NormalForce * frictions[0].sidewayStaticMy); // Max
        sidewayFriction = Mathf.Clamp(sidewayFriction, -Mathf.Abs(sidewayFrictionMax), Mathf.Abs(sidewayFrictionMax));
        
        wheel.sidewayForce = -Mathf.Sign(wheel.sidewaySpeed) * sidewayFriction;
       

        // Forward Friction
        float forwardFrictionMax = (.5f * rigidbody.mass * (wheel.totalSpeed.magnitude * wheel.totalSpeed.magnitude));

        float forwardFriction = Mathf.Abs(NormalForce * frictions[0].rollingMy);
        forwardFriction = Mathf.Clamp(forwardFriction, -Mathf.Abs(forwardFrictionMax), Mathf.Abs(forwardFrictionMax));

        wheel.forwardForce = (wheel.motorTorque/wheel.tireRadius)*10f + -Mathf.Sign(wheel.forwardSpeed) * forwardFriction;
        


        wheel.previousPosition = WheelObject.transform.position;

    }

    public void ApplyForces()
    {
        if (!wheel.IsGrounded) return;
        // Suspension
        rigidbody.AddForceAtPosition(wheel.hit.normal * suspension.TotalForce, WheelObject.transform.position);

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

        q.y += Quaternion.Euler(0, wheel.steerAngle, 0).y;

        rotation = q;
    }

    public void DrawDebug()
    {

        suspensionSlider.value = 1 - suspension.Compression / (wheel.tireRadius + suspension.Distance);

        if (!showDebug) return;
        // CurrentSuspension.
        Debug.DrawLine(
            WheelObject.transform.position,
            WheelObject.transform.position - new Vector3(0, suspension.Distance - suspension.Compression + wheel.tireRadius, 0), 
            Color.green);

        // Speed Vectors
        Vector3 origin = wheel.hit.point;
        Debug.DrawLine(origin, origin + wheel.Visual.transform.right * wheel.sidewayForce, Color.red); // SidewayFriction
        Debug.DrawLine(origin, origin + wheel.Visual.transform.forward * wheel.forwardForce, Color.blue); // Forward Force

        
    }

    #region Classes
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
        public float rpm = 0.0f;

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
        public Friction currentFriction = new Friction();
        public float forwardMy
        {
            get
            {
                if (totalSpeed.magnitude <= 1.0f) return currentFriction.staticMy;
                else if (isSpinning) return currentFriction.dynamicMy;
                return currentFriction.rollingMy;
            }
        }
        public float sidewayMy
        {
            get
            {
                if (totalSpeed.magnitude <= 1.0f) return currentFriction.sidewayStaticMy;
                else if (isSpinning) return currentFriction.sidewayDynamicMy;
                return currentFriction.rollingMy;
            }
        }

        //**********
        //Visuals
        //**********
        public GameObject Visual;


        // Debug
        public override string ToString()
        {
            return name + ": a: " + acceleration + " totalSpeed: " + totalSpeed.magnitude;
        }

    }

    [System.Serializable]
    public class Suspension
    {

        public float SpringForce = 25000.0f;
        public float Damper = 3500.0f;
        public float Distance = 0.3f;
        [Range(0.0f, 1.0f)] public float targetPosition = 0.5f;

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

        [Header("Friction Coefficient")]
        // Forward
        [Header("Forward")]
        public float staticMy = 0.7f;
        public float rollingMy = 0.03f;
        public float dynamicMy = 0.3f;

        // Sideways
        [Header("Sideways")]
        public float sidewayStaticMy = 0.7f;
        public float sidewayRollingMy = 0.7f;
        public float sidewayDynamicMy = 0.7f;


        public static Friction[] list;

        public Friction()
        {
            staticMy = .7f;
            rollingMy = .03f;
            dynamicMy = .3f;

            sidewayStaticMy = .7f;
            sidewayRollingMy = .7f;
            sidewayDynamicMy = .7f;
        }

    }

    #endregion

}
