using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VehiclePhysics {
    
    public class WheelData : MonoBehaviour
    {

        public new Rigidbody rigidbody;

        [HideInInspector]
        public WheelColliderHT coll;

        // 
        // Data about the Wheel
        // 

        [Tooltip("Radius of the whole Wheel. si: m")]
        public float tireRadius = 0.5f;

        [Tooltip("The mass of the wheel. si: kg")]
        public float mass = 25f;

        [Tooltip("The width of the wheel.")]
        public float width = 0.5f;

        public Transform visual;

        public Suspension suspension;

        public Friction friction;

        // Hidden in Inspector

        // Use to affect Wheel.
        [HideInInspector]
        public float motorTorque;

        [HideInInspector]
        public float steerAngle;

        /// <summary>
        /// Z is the direction the object is pointing.
        /// Acceleration in m/s.
        /// </summary>
        [HideInInspector]
        public Vector3 acceleration = Vector3.zero; // si: m/s

        [HideInInspector]
        public Vector3 previousAcceleration = Vector3.zero;

        
        private Vector3 velocity = Vector3.zero;
        /// <summary>
        /// Z is direction the object is pointing.
        /// Velocity in m/s.
        /// </summary>
        public Vector3 Velocity
        {
            get { return velocity; }
            set
            {
                previousVelocity = velocity;
                velocity = value;
            }
        }

        /// <summary>
        /// Z is direction the object is pointing.
        /// Velocity in m/s.
        /// </summary>
        [HideInInspector]
        public Vector3 previousVelocity = Vector3.zero;

        [HideInInspector]
        public Vector3 previousPosition = Vector3.zero;

        [HideInInspector]
        public RaycastHit hit;

        [HideInInspector]
        public bool isGrounded = false;

        [HideInInspector]
        public bool isSlipping = false;

        public float forwardMy
        {
            get
            {/*
                if (velocity.magnitude <= 1f) Debug.Log("Static");
                else if (isSlipping) Debug.Log("Dynamic");
                else Debug.Log("Rolling");
                */
                if (Velocity.magnitude <= 1f) return friction.forward.Static;
                else if (isSlipping) return friction.forward.Dynamic;
                return friction.forward.Rolling;
            }
        }
        public float sidewayMy
        {
            get
            {
                if (Velocity.magnitude <= 1f) return friction.sideway.Static;
                else if (isSlipping) return friction.sideway.Dynamic;
                return friction.sideway.Rolling;
            }
        }


        public float tireCircumfence
        {
            get { return 2f * tireRadius * Mathf.PI; }
        }

        public float rpm
        {
            get { return (Velocity.z / tireCircumfence) * 60f; }
        }


        void Start()
        {
            rigidbody = (rigidbody != null) ? rigidbody : GetComponentInParent<Rigidbody>();
            if (visual == null) visual = transform.GetChild(0);

            coll = new WheelColliderHT();
            coll.data = this;
        }

        private void Update()
        {
            Debug.DrawLine(transform.position, transform.position + Velocity * 5f);
           
        }

        ~WheelData()
        {
            coll = null;
        }

    }
}
