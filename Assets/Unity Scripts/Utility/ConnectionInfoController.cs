using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConnectionInfoController : MonoBehaviour {

	public uint averageSize = 30;
	public float showThreshold = 1;

	public ConnectionInfoDisplay leftTemple;
	public ConnectionInfoDisplay leftForehead;
	public ConnectionInfoDisplay rightForehead;
	public ConnectionInfoDisplay rightTemple;

	public bool showStatus { 
		get { return _showStatus; }
		set {
			if (value != _showStatus) {
				_showStatus = value;
				leftTemple.gameObject.SetActive(value);
				leftForehead.gameObject.SetActive(value);
				rightForehead.gameObject.SetActive(value);
				rightTemple.gameObject.SetActive(value);
			}
			// noop otherwise
		}
	}

	bool _showStatus = false;
	double _lastStatus = 0.0;

	SharpBCI.MovingAverageFilter[] filters;

	void Start() {
		filters = new SharpBCI.MovingAverageFilter[4];
		for (int i = 0; i < 4; i++) {
			filters[i] = new SharpBCI.MovingAverageFilter(averageSize);
		}
	}

	// Update is called once per frame
	void Update() {
		var status = (double[])SharpBCIController.BCI.connectionStatus.Clone();
		for (int i = 0; i < status.Length; i++) {
			status[i] = filters[i].Filter(status[i]);
		}

		leftTemple.AddContactQuality((int)status[0]);
		leftForehead.AddContactQuality((int)status[1]);
		rightForehead.AddContactQuality((int)status[2]);
		rightTemple.AddContactQuality((int)status[3]);

		var nextStatus = status.Sum();
		if (!showStatus && nextStatus - _lastStatus > showThreshold) {
			showStatus = true;
			_lastStatus = nextStatus;
		}
	}
}
