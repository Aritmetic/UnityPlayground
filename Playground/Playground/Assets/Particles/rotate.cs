using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotate : MonoBehaviour {

    public GameObject target;

    [Range(0.0f, 1f)] public float speed = 1f;

    private float distance;
    private float p = 0f;

	// Use this for initialization
	void Start () {
        distance = Vector3.Distance(transform.position, target.transform.position);
	}
	
	// Update is called once per frame
	void Update () {

        target.transform.position = transform.position + new Vector3(
            distance * Mathf.Sin(p),
            0f,
            distance * Mathf.Cos(p)
            );

        p += Time.deltaTime * speed;

	}
}
