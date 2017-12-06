using System;
using System.Linq;
using UnityEngine;

public class ConnectionInfoController : MonoBehaviour {

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

	// Update is called once per frame
	void Update () {
		var status = SharpBCIController.BCI.connectionStatus;
		leftTemple.AddContactQuality((int)status[0]);
		leftForehead.AddContactQuality((int)status[1]);
		rightForehead.AddContactQuality((int)status[2]);
		rightTemple.AddContactQuality((int)status[3]);

		var nextStatus = status.Sum();
		if (!showStatus && Math.Abs(_lastStatus - nextStatus) > 0.1) {
			showStatus = true;
			nextStatus = _lastStatus;
		}
	}
}
