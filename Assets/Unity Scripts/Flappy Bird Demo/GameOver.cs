using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour {
	public Text scoreLabel;
	double timer = 10.0;
	public string gameOverScene = "start-room";

	void Start()
	{
		string score = PlayerPrefs.GetInt ("score").ToString();
		scoreLabel.text = score;
	}

	void Update()
	{
		timer -= Time.deltaTime;
		if (timer <= 0) {
			SceneManager.LoadScene(gameOverScene);
		}
	}
}
