using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour {

	public float forwardSpeed;
	
	// Update is called once per frame
	void Update () {
		transform.position += transform.forward * forwardSpeed * Time.deltaTime;	
	}
}
