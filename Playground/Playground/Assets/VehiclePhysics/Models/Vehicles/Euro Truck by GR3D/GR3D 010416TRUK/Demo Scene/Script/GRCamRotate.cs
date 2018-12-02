using UnityEngine;
using System.Collections;

public class GRCamRotate : MonoBehaviour {

	void Update () {
		transform.Rotate(Vector3.up * 0.3f);
	}
}
