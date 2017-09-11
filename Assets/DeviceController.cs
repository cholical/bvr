using SharpBCI;
using UnityEngine;

public class DeviceController : MonoBehaviour {

	public const int OSC_DATA_PORT = 5000;

	public static EEGDeviceAdapter Device;

	// Use this for initialization
	void Awake() {
		Device = new RemoteOSCAdapter(OSC_DATA_PORT);
		Device.Start();
	}

	void OnDestroy() {
		Device.Stop();
	}

	void Update() {
		Device.FlushEvents();
	}
}
