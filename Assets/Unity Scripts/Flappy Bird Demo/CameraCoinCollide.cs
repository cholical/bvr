using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
 * CameraCoinCollide attaches to the Camera Rig, and it controlls collisions with coin objects and incrementing the game 
 * score whenever a coin collision occurs.
 * */
public class CameraCoinCollide : MonoBehaviour {

	//Total score of all the coins the user has retrieved 
	public static int coinScore;

	//Metallic sound asset for when a coin collision occurs
	public AudioClip coinSound;
	//"Ouch" sound asset for when user makes contact with the ground
	public AudioClip ouchSound;

	//Seconds before "Ouch" sound is replayed and score is re-decremented
	public float ouchInterval = 1.0f;

	float lastOuch = 0.0f;
	bool colliding = false;

	public CameraCoinCollide(){
	}

	//Handles when a coin collision occurs
	void OnTriggerEnter(Collider other) {
		AudioSource.PlayClipAtPoint(coinSound, transform.position);
		coinScore++;
		Destroy(other.gameObject);
	}
		
	//Establishes floor colliding condition as true for the Update loop
	void OnCollisionEnter(Collision collision) {
		Debug.Log("Collision");
		colliding = true;
		lastOuch = ouchInterval;
		//coinScore++;
		//Destroy(collision.gameObject);
	}

	//Establishes floor colliding condition as false for the Update loop
	void OnCollisionExit(Collision coll) {
		Debug.Log("Exit collision");
		colliding = false;
	}

	void Start() {
		coinScore = 0;
	}

	void Update() {
		//Test if collision is occuring 
		if (colliding) {
			//Sets time interval for sound replay
			lastOuch += Time.deltaTime;
			if (lastOuch >= ouchInterval) {
				//Triggered when player makes contact with the ground. "Ouch" sound is played, and coin score is decremented. 
				AudioSource.PlayClipAtPoint(ouchSound, transform.position);
				coinScore--;
				lastOuch = 0.0f;
			}
		}
	}
}
