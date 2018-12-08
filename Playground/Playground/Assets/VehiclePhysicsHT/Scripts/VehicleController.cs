using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VehiclePhysics
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {

        public new Rigidbody rigidbody;

        public bool customCenterOfMass = false;
        public Vector3 centerOfMass;

        public float downforce;

        [Space(5)]

        public Engine engine;

        [Space(5)]

        public Transmission transmission;

        [Space(5)]

        public AxleData[] axles;


        #region Variables
        //
        private float steerInput;
        private float throttle;
        private float clutchInput;

        private float motorTorque;

        #endregion

        // Use this for initialization
        void Start()
        {
            // Set rigidbody when null.
            rigidbody = (rigidbody != null) ? rigidbody : GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {

            throttle = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");



            transmission.Update(axles[0].leftWheel.rpm * axles[0].finalRatio * transmission.getCurrentGearRatio());

            
        }

        private void FixedUpdate()
        {

            float drag = .5f * 1.2f * (rigidbody.velocity.magnitude * rigidbody.velocity.magnitude) * 0.8f * (2.07f * 2.07f);
           // Vector3 drag = rigidbody.velocity * (1f - Time.fixedDeltaTime * 0.7f);
          //  rigidbody.AddForce(drag * -transform.forward);
            /*
            //Debug.Log("fl: " + axles[0].leftWheel.coll.lastForwardForce + ", grounded: " + axles[0].leftWheel.isGrounded +
                "\n fr: " + axles[0].rightWheel.coll.lastForwardForce + ", grounded: " + axles[0].rightWheel.isGrounded +
                "bl: " + axles[1].leftWheel.coll.lastForwardForce + ", grounded: " + axles[1].leftWheel.isGrounded +
                "\n br: " + axles[1].rightWheel.coll.lastForwardForce + ", grounded: " + axles[1].leftWheel.isGrounded +
                "Drag: " + drag + "\n\n");*/

            motorTorque = throttle * engine.maxTorque * transmission.getCurrentGearRatio();

            for(int i = 0 ; i < axles.Length ; ++i)
            {
                AxleData axle = axles[i];
                axle.steerInput = steerInput;
                axle.motorTorque = motorTorque;
                axle.FixedAxleUpdate();
            }
        }
    }
}