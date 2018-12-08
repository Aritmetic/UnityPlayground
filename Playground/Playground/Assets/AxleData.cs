using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehiclePhysics
{
    public class AxleData : MonoBehaviour
    {

        /*************************
         *   Data
         ************************/

        [Range(-360f, 360f), Tooltip("")]
        public float steerAngle = 40f;

        [Range(0f, 1f), Tooltip("Defines how much the wheels are affected by the engine.")]
        public float torqueCoefficient = 1f;

        [Space(5), Header("Brakes")]
        public float brakeForceMax = 40000f;

        [Range(0f, 1f), Tooltip("Defines how much the brakes are affected.")]
        public float brakeCoefficient = 1f;

        [Range(0f, 1f), Tooltip("Defines how much the handbrake affects the brakes.")]
        public float handbrakeCoefficient = 1f;

        [Range(0f, 35f), Tooltip("The final Gear Ratio for the wheel.")]
        public float finalRatio = 3.9f;


        [Space(5), Header("Wheels")]

        public WheelData leftWheel;
        public WheelData rightWheel;


        #region Variables

        [HideInInspector]
        public float steerInput;
        public float motorTorque;


        #endregion

        public void FixedAxleUpdate()
        {
            leftWheel.steerAngle = steerAngle * steerInput;
            rightWheel.steerAngle = steerAngle * steerInput;

            leftWheel.motorTorque = (motorTorque * finalRatio) * torqueCoefficient;
            rightWheel.motorTorque = (motorTorque * finalRatio) * torqueCoefficient;

            leftWheel.coll.FixedUpdate();
            rightWheel.coll.FixedUpdate();
        }

    }
}