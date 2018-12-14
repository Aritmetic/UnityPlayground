using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehiclePhysics
{

    public class WheelColliderHT : VehicleComponent
    {

        public WheelData data;

        public float NormalForce;
        public float lateralForce;
        public float forwardForce;

        public override void Update()
        {

        }

        public override void FixedUpdate()
        {
            UpdateSuspension();
            CalculateForces();
            ApplyForces();
            UpdateVisuals();

            data.previousPosition = data.transform.position;
            data.previousAcceleration = data.acceleration;

            data.motorTorque = 0.0f;
        }

       

        private void UpdateSuspension()
        {

            Vector3 origin = data.transform.position;

            data.suspension.PreviousCompression = data.suspension.Compression;
            if(Physics.Raycast(origin, -data.transform.up, out data.hit, data.suspension.Distance + data.tireRadius)) 
            {
                data.isGrounded = true;
                data.suspension.Compression = data.suspension.Distance + data.tireRadius - (data.hit.point - data.transform.position).magnitude;
            } else
            {
                data.isGrounded = false;
                data.suspension.Compression = 0.0f;
            }

        }

        private void CalculateForces()
        {
            if(data.isGrounded)
            {
                CalculateSuspensionForce();
                CalculateFriction();
            } else
            {
                data.suspension.Force = 0.0f;
                data.suspension.DampingForce = 0.0f;
            }
        }

        private void CalculateSuspensionForce()
        {
            data.suspension.Force = (data.suspension.Compression - data.suspension.Distance * data.suspension.targetPosition) * data.suspension.SpringForce;
            data.suspension.DampingForce = ((data.suspension.Compression - data.suspension.PreviousCompression) / Time.fixedDeltaTime) * data.suspension.Damper;
        }

        private void CalculateFriction()
        {
            lateralForce = 0.0f;
            forwardForce = 0.0f;

            // Global velocity
            Vector3 velocity = (data.transform.position - data.previousPosition) / Time.fixedDeltaTime;

            // Rotate around negativ rotation to get forward velocity on Z axis.
            data.Velocity = Quaternion.Inverse(data.visual.transform.rotation) * velocity;
            
            data.acceleration = (data.Velocity - data.previousVelocity) / Time.fixedDeltaTime;

            float tireForce = data.motorTorque / data.tireRadius;

            Vector3 forceLimiter = Vector3.zero;
            forceLimiter.x = ((data.Velocity.x - data.previousVelocity.x) / Time.fixedDeltaTime) * data.rigidbody.mass;
            forceLimiter.y = ((data.Velocity.y - data.previousVelocity.y) / Time.fixedDeltaTime) * data.rigidbody.mass;
            forceLimiter.z = ((data.Velocity.z - data.previousVelocity.z) / Time.fixedDeltaTime) * data.rigidbody.mass;
            
            NormalForce = data.suspension.TotalForce; // (m * g) / 4. Force for each suspension.

            // Forward
            float forwardFriction = NormalForce * data.forwardMy;
            forwardFriction = Mathf.Clamp(forwardFriction, 0f, Mathf.Abs(forceLimiter.z));
            //if (-.1f <= data.Velocity.z && data.Velocity.z <= .1f) forwardFriction = Mathf.Abs(forceLimiter.z);
            if (Mathf.Abs(data.Velocity.z) <= 0.01f) forwardFriction = 0.0f;

            forwardForce = -Mathf.Sign(data.Velocity.z) * forwardFriction;

            // Lateral Friction
            float lateralFriction = NormalForce * data.sidewayMy;
            //if (-.2f <= data.Velocity.x && data.Velocity.x <= .2f) lateralFriction = Mathf.Abs(forceLimiter.x);
            lateralFriction = Mathf.Clamp(lateralFriction, 0f, Mathf.Abs(forceLimiter.x));
            if (Mathf.Abs(data.Velocity.x) <= 0.01f) lateralFriction = 0f;

            lateralForce = -Mathf.Sign(data.Velocity.x) * lateralFriction;
            

            Debug.Log("^" + forwardForce + "<-->: " + lateralForce + " v: " + data.Velocity + " a: " + data.acceleration);
            // Old Code
            /*
             // Lateral Friction.
             float lateralFrictionForceMax = Mathf.Abs(.5f * NormalForce * (data.velocity.x * data.velocity.x));

             float lateralFrictionForce = Mathf.Clamp(NormalForce * data.sidewayMy, 0.0f, lateralFrictionForceMax);

             lateralForce = -Mathf.Sign(data.velocity.x) * lateralFrictionForce;


             // Forward Friction.
             float forwardFrictionForceMax = Mathf.Abs(.5f * NormalForce * (data.velocity.z * data.velocity.z));

             float forwardFirctionForce = Mathf.Clamp(NormalForce * data.forwardMy, 0.0f, forwardFrictionForceMax);

             float motorForce = data.motorTorque / data.tireRadius;

             forwardForce = -Mathf.Sign(data.velocity.z) * forwardFirctionForce + motorForce;
             Debug.Log("Force: " + forwardForce);
             //(data.motorTorque) / data.tireRadius*/
        }

        private void ApplyForces()
        {
            // Apply Suspension Force
            data.rigidbody.AddForceAtPosition(data.hit.normal * data.suspension.TotalForce, data.hit.point);

            // Add Friction Force
            data.rigidbody.AddForceAtPosition(lateralForce * data.visual.transform.right, data.hit.point);
            data.rigidbody.AddForceAtPosition(forwardForce * data.visual.transform.forward, data.hit.point);
            Debug.DrawLine(data.hit.point, data.hit.point + forwardForce * data.visual.transform.forward, Color.blue);
            Debug.DrawLine(data.hit.point, data.hit.point + lateralForce * data.visual.transform.right, Color.red);
        }

        public void UpdateVisuals()
        {
            Vector3 p;
            Quaternion q;

            GetWorldPos(out p, out q);

            data.visual.localPosition = p;
            data.visual.localRotation = q;
        }

        public void GetWorldPos(out Vector3 position, out Quaternion rotation)
        {
            position = new Vector3(0, -data.suspension.Distance + data.suspension.Compression, 0);
            Quaternion q = Quaternion.identity;

            q.y += Quaternion.Euler(0, data.steerAngle, 0).y;

            rotation = q;
        }

        ~WheelColliderHT()
        {
            data = null;
            MonoBehaviour.print("Deinit of WheelCollider.");
        }

        private int sign(float v)
        {
            if (v < 0) return -1;
            else if (0 < v) return 1;
            else return 1;
        }

    }
}
