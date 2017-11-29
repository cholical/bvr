using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeController : MonoBehaviour {
	double timer = 120.0;
	public string gameOverScene = "game-over";

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		timer -= Time.deltaTime;
		if (timer <= 0) {
			PlayerPrefs.SetInt ("score", CameraCoinCollide.coinScore);
			SceneManager.LoadScene(gameOverScene);
		}
	}
}
