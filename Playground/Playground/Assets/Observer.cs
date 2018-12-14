using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Observer : MonoBehaviour {


    public float v;
    public float a;

    private Rigidbody r;

	// Use this for initialization
	void Start () {
        r = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        a = (r.velocity.magnitude - v) / Time.deltaTime;
        v = r.velocity.magnitude;

	}
}
