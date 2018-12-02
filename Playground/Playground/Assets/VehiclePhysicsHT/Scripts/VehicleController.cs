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
            


        }

        private void FixedUpdate()
        {
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