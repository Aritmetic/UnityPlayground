using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEmitterTest : MonoBehaviour {

	public ParticleSystem effect;
    [Range(.1f, 3f)] public float speedModifier = 1f;

    private float startLifetimeConstantMin;
    private float startLifetimeConstantMax;

    private float startSpeedConstantMin;
    private float starSpeedConstantMax;

	// Use this for initialization
	void Start () {
        if (effect == null) effect = GetComponent<ParticleSystem>();

        startLifetimeConstantMin = effect.main.startLifetime.constantMin;
        startLifetimeConstantMax = effect.main.startLifetime.constantMax;

        startSpeedConstantMin = effect.main.startSpeed.constantMin;
        starSpeedConstantMax = effect.main.startSpeed.constantMax;
        
    }
	
	// Update is called once per frame
	void Update () {

        float verticalInput = Input.GetAxis("Vertical");

        ParticleSystem.EmissionModule em = effect.emission;
		float v = 70f + 70f * verticalInput;
		//em.rateOverTime = v;

        ParticleSystem.MainModule m = effect.main;

        m.startLifetime = new ParticleSystem.MinMaxCurve(
            startLifetimeConstantMin * (1 - verticalInput) + startLifetimeConstantMin * (speedModifier) * verticalInput,
            startLifetimeConstantMax * (1 - verticalInput) + startLifetimeConstantMax * (speedModifier) * verticalInput
            );

        m.startSpeed = new ParticleSystem.MinMaxCurve(
            startSpeedConstantMin * (1-verticalInput) + startSpeedConstantMin * (speedModifier) * verticalInput,
            starSpeedConstantMax * (1-verticalInput) + starSpeedConstantMax * (speedModifier) * verticalInput
            );
       
	}
}
