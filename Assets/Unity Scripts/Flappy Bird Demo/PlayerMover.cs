using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour {

	public float movementRange = 1.75f;
	public float timeFactor = 7.5f;

	Vector3 originalPos;

	void Start() {
		originalPos = transform.position;	
	}

	// Update is called once per frame
	void Update () {
		transform.position = originalPos + Vector3.up * movementRange * Mathf.Sin (2 * Mathf.PI * (Time.time / timeFactor));
	}
}
