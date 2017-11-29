using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCoinCollide : MonoBehaviour {
	public static int coinScore;
	public AudioClip coinSound;
	public AudioClip ouchSound;

	public CameraCoinCollide(){
	}

	void OnTriggerEnter(Collider other) {
		//print("Trigger");
		AudioSource.PlayClipAtPoint(coinSound, transform.position);
		coinScore++;
		Destroy(other.gameObject);
	}

	void onCollision(){
		AudioSource.PlayClipAtPoint(ouchSound, transform.position);
		coinScore--;

	}

	//void OnCollisionEnter(Collision collision) {
	//	print ("Collision");
	//	coinScore++;
	//	Destroy(collision.gameObject);
	//}
		
}
