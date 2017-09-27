using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectedDisplay : MonoBehaviour {

	readonly static string[] nodeNames = new string[] { 
		"Left Temple",
		"Left Forehead",
		"Right Forehead",
		"Right Temple",
	};

	Text connectedText;

	// Use this for initialization
	void Start () {
		connectedText = GetComponent<Text>();	
	}
	
	// Update is called once per frame
	void Update () {
		var connectionState = SharpBCIController.BCI.connectionStatus;
		var connectionString = "";
		for (var i = 0; i < 4; i++) {
			var status = (int)connectionState [i];
			if (status == 4) {
				if (i > 0)
					connectionString += ", ";
				connectionString += nodeNames [i] + ": bad";
			} else if (status == 2) {
				if (i > 0)
					connectionString += ", ";
				connectionString += nodeNames [i] + ": poor";
			} else {
				if (i > 0)
					connectionString += ", ";
				connectionString += nodeNames [i] + ": good";
			}
		}
		if (connectedText.text != connectionString)
			connectedText.text = connectionString;
	}
}
