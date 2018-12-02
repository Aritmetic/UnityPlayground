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

        public float lastLateralForce;

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

            if (lateralForce != 0f) lastLateralForce = lateralForce;

            lateralForce = 0.0f;
            forwardForce = 0.0f;
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
            // Global velocity
            Vector3 velocity = (data.transform.position - data.previousPosition) / Time.fixedDeltaTime;
           
            // Rotate around negativ rotation to get forward velocity on Z axis.
            data.velocity = Quaternion.Inverse(data.visual.transform.rotation) * velocity;
            
            data.acceleration = (data.velocity - data.previousVelocity) / Time.fixedDeltaTime;


            NormalForce = data.suspension.TotalForce + Mathf.Abs(data.rigidbody.mass * Physics.gravity.y);


            // Lateral Friction.
            float lateralFrictionForceMax = Mathf.Abs(.5f * NormalForce * (data.velocity.x * data.velocity.x));

            float lateralFrictionForce = Mathf.Clamp(NormalForce * data.sidewayMy, 0.0f, lateralFrictionForceMax);

            lateralForce = -Mathf.Sign(data.velocity.x) * lateralFrictionForce;


            // Forward Friction.
            float forwardFrictionForceMax = Mathf.Abs(.5f * data.rigidbody.mass * (data.velocity.z * data.velocity.z));

            float forwardFirctionForce = Mathf.Clamp(NormalForce * data.forwardMy, 0.0f, forwardFrictionForceMax);

            forwardForce = (data.motorTorque / data.tireRadius) + -Mathf.Sign(data.velocity.z) * forwardFirctionForce;

        }

        private void ApplyForces()
        {
            // Apply Suspension Force
            data.rigidbody.AddForceAtPosition(data.hit.normal * data.suspension.TotalForce, data.hit.point);

            // Add Friction Force
            data.rigidbody.AddForceAtPosition(lateralForce * data.visual.transform.right, data.hit.point);
            data.rigidbody.AddForceAtPosition(forwardForce * data.visual.transform.forward, data.hit.point);
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

    }
}
