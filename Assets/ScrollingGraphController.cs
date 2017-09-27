using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class ScrollingGraphController : MonoBehaviour {

	public EEGDataType type;

	ScrollingGraph graph;

	bool startCalled = false;

	void Start() {
		startCalled = true;
		graph = GetComponent<ScrollingGraph>();
		SharpBCIController.BCI.AddRawHandler(type, OnEEGData);
	}

	// Use this for initialization
	void OnEnable() {
		if (!startCalled)
			return;

		SharpBCIController.BCI.AddRawHandler(type, OnEEGData);
	}

	void OnDisable() {
		SharpBCIController.BCI.RemoveRawHandler(type, OnEEGData);
	}

	Queue<EEGEvent> evts = new Queue<EEGEvent>();

	void Update() {
		while (evts.Count > 0) {
			graph.AppendValue(evts.Dequeue());
		}
	}

	void OnEEGData(EEGEvent evt) {
		evts.Enqueue(evt);
	}
}
