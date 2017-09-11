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
		DeviceController.Device.AddHandler(type, OnEEGData);
	}

	// Use this for initialization
	void OnEnable() {
		if (!startCalled)
			return;
		
		DeviceController.Device.AddHandler(type, OnEEGData);
	}

	void OnDisable() {
		DeviceController.Device.RemoveHandler(type, OnEEGData);
	}

	void OnEEGData(EEGEvent evt) {
		graph.AppendValue(evt);
	}
}
