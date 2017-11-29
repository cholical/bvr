using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinManager : MonoBehaviour {
	public Text scoreLabel;

	void Start()
	{
		scoreLabel.text = "0";
	}

	void Update()
	{
		scoreLabel.text = CameraCoinCollide.coinScore.ToString();
	}
		

}
