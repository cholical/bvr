using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour {

	public float movementRange = 1.75f;
	public float timeFactor = 7.5f;
	public float forwardSpeed = 5f;
	public float teleportDist = 50f;

	Vector3 originalPos;

	void Start() {
		originalPos = transform.position;	
	}

	// Update is called once per frame
	void Update () {
		//if (!useSharpBCI) {
		//	transform.position = originalPos + Vector3.up * movementRange * Mathf.Sin(2 * Mathf.PI * (Time.time / timeFactor));
		//}
		transform.position += Vector3.forward * forwardSpeed * Time.deltaTime;
		if (Mathf.Abs(transform.position.z - originalPos.z) >= teleportDist) {
			var pos = transform.position;
			pos.z -= teleportDist;
			transform.position = pos;
		}
	}
}
