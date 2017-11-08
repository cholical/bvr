using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCoinCollide : MonoBehaviour {
	public static int coinScore;

	public CameraCoinCollide(){
	}

	void OnCollisionEnter(Collision collision)
	{
		print ("Collision");
		coinScore++;
		Destroy(collision.gameObject);
	}
		
}
