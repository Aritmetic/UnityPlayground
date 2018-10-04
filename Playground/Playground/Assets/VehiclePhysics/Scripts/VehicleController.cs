using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour {

    public Rigidbody rigidbody;
    public bool customCenterOfMass = false;
    public Vector3 centerOfMass;

	// Use this for initialization
	void Start () {
        Initialize();
	}
	
	// Update is called once per frame
	void Update () {
        rigidbody.centerOfMass = centerOfMass;
	}

    [ContextMenu("Init")]
    public void Initialize()
    {
        if (rigidbody == null) gameObject.AddComponent<Rigidbody>();

        rigidbody = GetComponent<Rigidbody>();
        if (customCenterOfMass)
            rigidbody.centerOfMass = centerOfMass;
        else
            centerOfMass = rigidbody.centerOfMass;
    }
}
