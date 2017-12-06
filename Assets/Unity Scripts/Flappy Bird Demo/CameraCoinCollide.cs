using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCoinCollide : MonoBehaviour {

	public static int coinScore;

	public AudioClip coinSound;
	public AudioClip ouchSound;

	public float ouchInterval = 1.0f;

	float lastOuch = 0.0f;
	bool colliding = false;

	public CameraCoinCollide(){
	}

	void OnTriggerEnter(Collider other) {
		//print("Trigger");
		AudioSource.PlayClipAtPoint(coinSound, transform.position);
		coinScore++;
		Destroy(other.gameObject);
	}
		
	void OnCollisionEnter(Collision collision) {
		Debug.Log("Collision");
		colliding = true;
		lastOuch = ouchInterval;
		//coinScore++;
		//Destroy(collision.gameObject);
	}

	void OnCollisionExit(Collision coll) {
		Debug.Log("Exit collision");
		colliding = false;
	}
		
	void Update() {
		if (colliding) {
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
