using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FFTTrainingController : MonoBehaviour {

	public int trainingId = -1;

	bool training;

	public void ToggleTraining() {
		if (training) {
			SharpBCIController.BCI.StopTraining(trainingId);
			GetComponentInChildren<Text>().text = "Start Training #" + trainingId;
		} else {
			SharpBCIController.BCI.StartTraining(trainingId);
			GetComponentInChildren<Text>().text = "Stop Training #" + trainingId;
		}
		training = !training;
	}
}
