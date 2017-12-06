using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SteamVR_TrackedObject))]
public class ConnectionInfoToggle : MonoBehaviour {

	public ConnectionInfoController connectionInfoController;

	SteamVR_TrackedObject trackedObj;

	SteamVR_Controller.Device Controller {
		get {
			return SteamVR_Controller.Input((int)trackedObj.index);
		}
	}

	void Awake() {
		//UnityEngine.VR.InputTracking.disablePositionalTracking = true;
		trackedObj = GetComponent<SteamVR_TrackedObject>();
	}

	void Update() {
		if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu)) {
			connectionInfoController.showStatus = !connectionInfoController.showStatus;
		}
	}
}
