using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/**
 * CoinManager takes in the coin score from CameraCoinCollide and generates it as text to display on the screen
 */

public class CoinManager : MonoBehaviour {
	public Text scoreLabel;

	void Start()
	{
		//Start text is 0
		scoreLabel.text = "0";
	}

	void Update()
	{
		//Prepares for printing on the game scene
		scoreLabel.text = CameraCoinCollide.coinScore.ToString();
	}
		

}
