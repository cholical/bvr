using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SharpBCI;

public class TrainingController : MonoBehaviour {

	public Text trainingStatus;
	public Button trainingBtn;

	public Text eventText;

	public Queue<TrainedEvent> lastEvents = new Queue<TrainedEvent>();
	public int retainedEvents = 5;

	int lastId = 0;
	bool eventsDirty = false;
	bool isTraining = false;

	// Use this for initialization
	void Start () {
		eventText.text = "No training events";
		trainingBtn.onClick.AddListener(delegate {
			if (isTraining) {
				trainingStatus.text = "Currently not training";
				trainingBtn.gameObject.GetComponentInChildren<Text>().text = "Start Training";
				SharpBCIController.BCI.StopTraining(lastId);
				SharpBCIController.BCI.AddTrainedHandler(lastId, delegate (TrainedEvent evt) {
					lastEvents.Enqueue(evt);
					if (lastEvents.Count > retainedEvents) {
						lastEvents.Dequeue();
					}
					eventsDirty = true;
				});
				lastId++;
			} else {
				SharpBCIController.BCI.StartTraining(lastId);
				trainingStatus.text = "Currently training";
				trainingBtn.gameObject.GetComponentInChildren<Text>().text = "Stop Training";
			}
			isTraining = !isTraining;
		});
	}

	void UpdateEventText() {
		var s = "";
		foreach (var evt in lastEvents) {
			s += string.Format("Event {0},", evt.id);
		}
		eventText.text = s;
	}
	
	// Update is called once per frame
	void Update () {
		if (eventsDirty) UpdateEventText();
	}
}
