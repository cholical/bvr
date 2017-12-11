using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/**
 * TimeController sets the time of a game of Flying Birds, currently set at two minutes, or 120 seconds. After that interval,
 * the Game Over scene is loaded. Before ending, the player's coin score is saved as PlayerPrefs. 
 */
public class TimeController : MonoBehaviour {
	//Time in seconds the game will last
	double timer = 120.0;
	//Scene to be loaded once timer reaches <= 0
	public string gameOverScene = "game-over";

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		//Decrements elapsed time from the total time
		timer -= Time.deltaTime;
		if (timer <= 0) {
			//Saves user coin score to use in Game Over scene
			PlayerPrefs.SetInt ("score", CameraCoinCollide.coinScore);
			//Loads Game Over
			SceneManager.LoadScene(gameOverScene);
		}
	}
}
