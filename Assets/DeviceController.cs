﻿using SharpBCI;
using UnityEngine;
using System.Diagnostics;

public class DeviceController : MonoBehaviour {

	public const int OSC_DATA_PORT = 5000;

	public static EEGDeviceAdapter Device;

	Process museIOProcess;

	// Use this for initialization
	void Awake() {

		// start Muse-IO
		try {
			museIOProcess = new Process();
			museIOProcess.StartInfo.FileName = System.IO.Path.Combine(Application.streamingAssetsPath, "muse-io.exe");
			// default is osc.tcp://localhost:5000, but we expect udp
			museIOProcess.StartInfo.Arguments = "--osc osc.udp://localhost:5000";
			museIOProcess.StartInfo.CreateNoWindow = true;
			museIOProcess.StartInfo.UseShellExecute = false;
			museIOProcess.Start();
		} catch (System.Exception e) {
			UnityEngine.Debug.LogError("Could not open muse-io:");
			UnityEngine.Debug.LogException(e);
		}

		Device = new RemoteOSCAdapter(OSC_DATA_PORT);
		Device.Start();
	}

	void OnDestroy() {
		museIOProcess.Kill();
		museIOProcess.WaitForExit();
		Device.Stop();
	}

	void Update() {
		Device.FlushEvents();
	}
}
