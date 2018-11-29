using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody)), ExecuteInEditMode]
public class VehicleController : MonoBehaviour {

    public new Rigidbody rigidbody;
    public bool customCenterOfMass = false;
    public Vector3 centerOfMass;
    
    [Space(5)]

    public Steering steering;

    [Space(5)]

    public Engine engine;

    [Space(5)]

    public Transmission transmission;

    [Space(5)]

    public Axle[] axles;
    //public WheelColliderVP[] colls;

    [Space(5), Header("Aerodynamics")]

    public Spoiler[] wings;
    
	// Use this for initialization
	void Start () {

        rigidbody = GetComponent<Rigidbody>();

        rigidbody.centerOfMass = centerOfMass;

        foreach (Axle a in axles)
            a.setRigidbody(rigidbody);
        
	}

    // Update is called once per frame
    void FixedUpdate() {

        rigidbody.centerOfMass = centerOfMass;

        float sqaureVelocity = rigidbody.velocity.magnitude * rigidbody.velocity.magnitude;
        
        float steerInput = Input.GetAxis("Horizontal");
        float throttle = Input.GetAxis("Vertical");
        //float clutch = Input.GetAxis("Clutch");

        // #to-do: find a way to calculate the current torque. Formula should make a nice torque curve. use curves in transmission
        // # Animation curves in Transission? !!!
        float motorTorque = throttle * engine.maxTorque * transmission.getCurrentGearRatio();
        
        //********************
        // Apply Downforces
        //*********************
        for (int i = 0; i < wings.Length; ++i)
        {
            rigidbody.AddForceAtPosition(-transform.up * wings[i].GetDownForce(sqaureVelocity), wings[i].transform.position);
        }

        //*******************
        // Handle Wheels
        //*******************
        float highestWheelRpm = 0f;
        for (int i = 0; i < axles.Length; ++i)
        {
            Axle axle = axles[i];

            axle.steerInput = steerInput;
            axle.motorTorque = motorTorque;
            axle.FixedUpdate();

            float rpm = axle.leftWheel.wheel.RPM * axle.GearRatio;

            highestWheelRpm = (highestWheelRpm < rpm) ? rpm : highestWheelRpm;
        }
        highestWheelRpm *= transmission.getCurrentGearRatio();
        
        engine.currentRpm = highestWheelRpm;
	}

    #region Initilizer

    [ContextMenu("Init Ackermann")]
    public void initAckermann()
    {
        foreach(Axle axle in axles)
        {
            if (axle != axles[axles.Length - 1])
            axle.geometry.ackermann = Mathf.Abs(axles[axles.Length - 1].leftWheel.WheelObject.transform.localPosition.z) + Mathf.Abs(axle.leftWheel.WheelObject.transform.localPosition.z);
        }
    }

    
   [ContextMenu("Set Front Wheels To All Other Wheels")]
   public void setFrontWheelLeftToAllOtherWheels()
    {
        
        foreach(Axle axle in axles)
        {
            axle.leftWheel.wheel = axles[0].leftWheel.wheel;
            axle.rightWheel.wheel = axles[0].leftWheel.wheel;

            //axle.leftWheel.wheel;
        }
    }

    #endregion

    #region Classes

    [System.Serializable]
    public class Spoiler // #todo -> rename to Wing
    {
        // https://en.wikipedia.org/wiki/Downforce
        // http://www.ppl-flight-training.com/lift-formula.html

        public Transform transform;

        [Tooltip("Co-Effiecient of Lift")]
        public float CL = 2.0f;
        [Tooltip("Surface Area")]
        public float s = 2f;

        public float mult;

        /// <summary>
        /// Air density.
        /// </summary>
        public const float p = 1.255f;
        public const float pHalf = .62775f;

        public float GetDownForce(float vsquare)
        {
            return CL * 1/2 * p * vsquare * s * mult;
        }
    }

    #endregion

    #region Helper
    
    [ContextMenu("Set Ackermann")]
    private void setAckermann()
    {
        float ackermannPoint = -1 * getAckermanPoint();

        foreach(Axle axle in axles)
        {
            if (axle.steerAngleMax < 0.05f) continue;
            axle.geometry.ackermann = ackermannPoint + axle.leftWheel.WheelObject.transform.localPosition.z;
        }
    }

    [ContextMenu("Set Front Left as Master")]
    public void applyFrontLeftToAllOtherWheels()
    {
        foreach(Axle axle in axles)
        {
            axle.leftWheel = axles[0].leftWheel;
            axle.rightWheel = axles[0].leftWheel;
            
        }
    }

    [ContextMenu("Find Wheels")]
    public void FindWheels()
    {
        Transform wheels = transform.Find("Wheels");
        if (wheels == null)
        {
            Debug.Log("No Wheels object found.");
            return;
        }
        
        axles = new Axle[wheels.childCount/2];


        int axleCount = 0;
        for(int i = 0; i < this.axles.Length; ++i)
        {
            Debug.Log("i: " + i + ", left: " + wheels.GetChild(axleCount).transform.gameObject.name + ", right: " + wheels.GetChild(axleCount + 1).transform.gameObject.name);
            this.axles[i].leftWheel.WheelObject = wheels.GetChild(axleCount).transform.gameObject;
            this.axles[i].rightWheel.WheelObject = wheels.GetChild(axleCount + 1).transform.gameObject;

            axleCount = axleCount + 2;
            
        }

    }

    private float getAckermanPoint()
    {
        int counter = 0;
        float distance = 0f;

        foreach(Axle axle in axles)
        {
            if (axle.steerAngleMax > 0.05f) continue;
            counter += 1;
            distance += axle.leftWheel.WheelObject.transform.localPosition.z;
        }

        return distance / counter;
    }

    #endregion

}
