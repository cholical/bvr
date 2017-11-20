using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TrainingTimeDisplay : MonoBehaviour {

	public FlappyBirdController flappyBirdController;
	public bool displayUp = true;

	Text textObj;
	string originalText;

	// Use this for initialization
	void Start () {
		textObj = GetComponent<Text>();
		originalText = textObj.text;
	}
	
	// Update is called once per frame
	void Update () {
		if (flappyBirdController.IsTraining) {
			textObj.text = originalText + string.Format("({0:P0})", (displayUp ? flappyBirdController.UpPercent : flappyBirdController.DownPercent));
		} else {
			textObj.text = originalText;
		}
	}
}
