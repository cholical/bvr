using System;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class ScrollingGraphController : MonoBehaviour {

	public EEGDataType type;
	public int windowSize = 256;

	Graph graph;

	bool startCalled = false;
	Queue<Vector2>[] data;
	bool dataDirty = false;
	DateTime started;

	void Start() {
		started = DateTime.UtcNow;
		data = new Queue<Vector2>[SharpBCIController.BCI.channels];
		for (int i = 0; i < data.Length; i++) {
			data[i] = new Queue<Vector2>();
		}
		startCalled = true;
		graph = GetComponent<Graph>();
		graph.SetNumSeries(data.Length);
		graph.yFormatter = (x) => string.Format("{0:F2}", x);
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

	void Update() {
		if (dataDirty) {
			for (int i = 0; i < data.Length; i++) {
				graph.SetData(i, data[i].ToArray());
			}
		}
	}

	void OnEEGData(EEGEvent evt) {
		for (int i = 0; i < evt.data.Length; i++) {
			data[i].Enqueue(new Vector2((float)DateTime.UtcNow.Subtract(started).TotalSeconds, (float)evt.data[i]));
			if (data[i].Count == windowSize + 1)
				data[i].Dequeue();
		}
		dataDirty = true;
	}
}
