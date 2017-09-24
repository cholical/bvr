using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SharpBCI;

public class EEGDataOutput : MonoBehaviour {

	public EEGDataType dataType = EEGDataType.EEG;
	public int updateSkips = 60;

	Text text;

	// Use this for initialization
	void Start () {
		text = GetComponent<Text>();
		SharpBCIController.BCI.AddRawHandler(dataType, OnEEGData);
	}

	void OnDestroy() {
		SharpBCIController.BCI.RemoveRawHandler(dataType, OnEEGData);
	}

	double[] buffer = new double[4];

	int nUpdates = 0;	
	void Update() {
		if (nUpdates % updateSkips == 0) {
			text.text = string.Format("TP9 {0:F2}, FL: {1:F2}, RL: {2:F2}, TP10: {3:F2}", buffer[0], buffer[1], buffer[2], buffer[3]);
		}
		nUpdates++;
	}

	void OnEEGData(EEGEvent evt) {
		buffer[0] = evt.data[0];
		buffer[1] = evt.data[1];
		buffer[2] = evt.data[2];
		buffer[3] = evt.data[3];
	}
}
