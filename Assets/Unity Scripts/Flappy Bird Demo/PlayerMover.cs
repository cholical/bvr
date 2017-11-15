using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour {

	public enum MovementMode {
		OSCILLATING = 0,
		UP = 1,
		DOWN = 2,
		NONE = 3,
	}

	public delegate void TeleportHandler();
	public event TeleportHandler OnPlayerTeleported;

	public float movementRange = 1.75f;
	public float timeFactor = 7.5f;

	public float upSpeed = 2.5f;
	public float forwardSpeed = 5f;
	public float teleportDist = 50f;

	public MovementMode movementMode { 
		get { return _movementMode; } 
		set { _movementMode = value; }
	}

	MovementMode _movementMode;
	Vector3 originalPos;

	void Start() {
		originalPos = transform.position;	
	}

	// Update is called once per frame
	void Update () {
		var pos = transform.position;
		switch (_movementMode) {
			case MovementMode.UP:
				pos.y += upSpeed * Time.deltaTime;
				break;
			case MovementMode.DOWN:
				pos.y -= upSpeed * Time.deltaTime;
				break;
			case MovementMode.OSCILLATING:
				pos.y = originalPos.y + movementRange * Mathf.Sin(2 * Mathf.PI * (Time.time / timeFactor));
				break;
		}

		pos += Vector3.forward * forwardSpeed * Time.deltaTime;

		if (Mathf.Abs(pos.z - originalPos.z) >= teleportDist) {
			pos.z -= teleportDist;
			if (OnPlayerTeleported != null)
				OnPlayerTeleported();
		}
		transform.position = pos;
	}
}
