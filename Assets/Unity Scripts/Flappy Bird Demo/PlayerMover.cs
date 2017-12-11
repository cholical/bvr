using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Controls player movement in up direction. All horizontal motion is controlled by the terrain and not the player.
 */
public class PlayerMover : MonoBehaviour {

	public float movementRange = 1.75f;
	public float timeFactor = 7.5f;

	//Player starts at edge of the terrain
	Vector3 originalPos;

	void Start() {
		originalPos = transform.position;	
	}

	// Update is called once per frame
	void Update () {
		transform.position = originalPos + Vector3.up * movementRange * Mathf.Sin (2 * Mathf.PI * (Time.time / timeFactor));
	}
}
