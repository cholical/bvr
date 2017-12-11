using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
/**
 * Controlls the Game Over scene after transitioning from the Flying Birds Demo.
 */
public class GameOver : MonoBehaviour {
	public Text scoreLabel;
	//Scene set to last 10 seconds
	double timer = 10.0;
	//Scene to load after timer reaches <= 0
	public string gameOverScene = "start-room";

	void Start()
	{
		//Get coin score and convert it into format able to be insertted into game
		string score = PlayerPrefs.GetInt ("score").ToString();
		scoreLabel.text = score;
	}

	void Update()
	{
		//Time before loading back the Start Room scene
		timer -= Time.deltaTime;
		if (timer <= 0) {
			SceneManager.LoadScene(gameOverScene);
		}
	}
}
