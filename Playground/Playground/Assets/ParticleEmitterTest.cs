using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEmitterTest : MonoBehaviour {

	public ParticleSystem effect;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		ParticleSystem.EmissionModule em = effect.emission;
		float v = 70f + 70f * Input.GetAxis("Vertical");
		em.rateOverTime = v;
	}
}
