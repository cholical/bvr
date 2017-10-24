using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class TrainingGraphController : MonoBehaviour {

	public int[] trainedEventIds;
	public int windowSize = 0;

	readonly Queue<Vector2> previousEvents = new Queue<Vector2>();

	DateTime started;
	Graph graph;
	bool graphDirty = false;

	// Use this for initialization
	void Start () {
		started = DateTime.UtcNow;
		graph = GetComponent<Graph>();
		graph.SetNumSeries(1);
		if (trainedEventIds == null) return;
		foreach (var id in trainedEventIds) {
			SharpBCIController.BCI.AddTrainedHandler(id, OnTrainedEvent);
		}
	}

	void OnDestroy() { 
		if (trainedEventIds == null) return;
		foreach (var id in trainedEventIds) {
			SharpBCIController.BCI.RemoveTrainedHandler(id, OnTrainedEvent);
		}
	}

	void Update() {
		if (graphDirty) {
			graph.SetData(0, previousEvents.ToArray());
			graphDirty = false;
		}
	}

	void OnTrainedEvent(TrainedEvent evt) {
		previousEvents.Enqueue(new Vector2((float) evt.time.Subtract(started).TotalSeconds, evt.id));
		if (previousEvents.Count == windowSize + 1) {
			previousEvents.Dequeue();
		}
		graphDirty = true;
	}
}
