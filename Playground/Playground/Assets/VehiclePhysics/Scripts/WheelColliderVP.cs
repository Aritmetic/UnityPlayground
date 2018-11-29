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
    public string currentFrictionName = "Default";

    [SerializeField]
    public Dictionary<string, Friction> frictions
    {
        get
        {
            return Friction.Instance.list;
        }
    }
    //Friction.Instance.list;

    [SerializeField]
    public Friction currentFriction;

    [HideInInspector]
    public Rigidbody rigidbody;
    
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

        float NormalForce = suspension.TotalForce ;//(rigidbody.mass * Physics.gravity.y)/4; //# todo suspension force need to be applied. Also DonwFOrce from wind.
        

        // Sideway Friction
        float sidewayFrictionMax = Mathf.Abs(.5f * rigidbody.mass * (wheel.sidewaySpeed * wheel.sidewaySpeed));
        
        float sidewayFriction = Mathf.Abs(NormalForce * frictions["Default"].sideway.Static); // Max sideway friction
        sidewayFriction = Mathf.Clamp(sidewayFriction, -sidewayFrictionMax, sidewayFrictionMax);
        
        wheel.sidewayForce = -Mathf.Sign(wheel.sidewaySpeed) * sidewayFriction;

        MonoBehaviour.print("Applied Force: " + wheel.sidewayForce + ", friction: " + sidewayFriction + ", max: " + sidewayFrictionMax 
            + "");

        // Forward Friction
        float forwardFrictionMax = (.5f * rigidbody.mass * (wheel.totalSpeed.magnitude * wheel.totalSpeed.magnitude));

        float forwardFriction = Mathf.Abs(NormalForce * frictions["Default"].forward.Rolling);
        forwardFriction = Mathf.Clamp(forwardFriction, -Mathf.Abs(forwardFrictionMax), Mathf.Abs(forwardFrictionMax));

        wheel.forwardForce = (wheel.motorTorque/wheel.tireRadius) + -Mathf.Sign(wheel.forwardSpeed) * forwardFriction;
        
        
        wheel.previousPosition = WheelObject.transform.position;

    }

    public void ApplyForces()
    {
        if (!wheel.IsGrounded) return;
        // Suspension
        rigidbody.AddForceAtPosition(wheel.hit.normal * suspension.TotalForce, WheelObject.transform.position);

        // Friction
        rigidbody.AddForceAtPosition(wheel.sidewayForce * wheel.Visual.transform.right, WheelObject.transform.position);
        rigidbody.AddForceAtPosition(wheel.forwardForce * wheel.Visual.transform.forward, WheelObject.transform.position);
        
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
        
        q.y += Quaternion.Euler(0, Mathf.Clamp(wheel.steerAngle, -90f, 90f), 0).y;
        
        rotation = q;
    }
}
