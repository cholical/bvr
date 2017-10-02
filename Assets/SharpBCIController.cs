﻿using SharpBCI;
using UnityEngine;
using System.Diagnostics;

public enum SharpBCIControllerType {
	MUSE,
	TONE_GENERATOR,
}

public class UnityLogger : ILogOutput {

	public void Dispose() {
		// no cleanup required
	}

	public void Log(LogLevel level, object message) {
		switch (level) {
			case LogLevel.INFO:
				UnityEngine.Debug.Log(message);
				break;
			case LogLevel.WARNING:
				UnityEngine.Debug.LogWarning(message);
				break;
			case LogLevel.ERROR:
				UnityEngine.Debug.LogError(message);
				break;
			default:
				return;
		}
	}
}

public class SharpBCIController : MonoBehaviour {

	public const int OSC_DATA_PORT = 5000;
	public const string LOG_NAME = "SharpBCI_log.txt";

	public static SharpBCI.SharpBCI BCI;

	public SharpBCIControllerType bciType;

	Process museIOProcess;

	// Use this for initialization
	void Awake() {
		// FileLogger requires actual pathnames not Unity
		string logName = System.IO.Path.Combine(Application.persistentDataPath.Replace('/', System.IO.Path.DirectorySeparatorChar), LOG_NAME);
		UnityEngine.Debug.Log("Writing sharpBCI log to: " + logName);
		// configure logging
		SharpBCI.Logger.AddLogOutput(new UnityLogger());
		SharpBCI.Logger.AddLogOutput(new FileLogger(logName));

		if (bciType == SharpBCIControllerType.MUSE) {
			// start Muse-IO
			try {
				museIOProcess = new Process();
				museIOProcess.StartInfo.FileName = System.IO.Path.Combine(Application.streamingAssetsPath, "muse-io.exe");
				// default is osc.tcp://localhost:5000, but we expect udp
				museIOProcess.StartInfo.Arguments = "--osc osc.udp://localhost:5000";
				museIOProcess.StartInfo.CreateNoWindow = true;
				museIOProcess.StartInfo.UseShellExecute = false;
				museIOProcess.Start();
				museIOProcess.PriorityClass = ProcessPriorityClass.RealTime;
			} catch (System.Exception e) {
				UnityEngine.Debug.LogError("Could not open muse-io:");
				UnityEngine.Debug.LogException(e);
			}

			EEGDeviceAdapter adapter = new RemoteOSCAdapter(OSC_DATA_PORT);
			BCI = new SharpBCIBuilder().EEGDeviceAdapter(adapter).Build();
		} else if (bciType == SharpBCIControllerType.TONE_GENERATOR) { 
			EEGDeviceAdapter adapter = new DummyAdapter(new double[] { 
				// alpha
				10, 
				//// beta
				24, 
				//// gamma
				40, 
				//// delta
				2, 
				//// theta
				6, 
			}, new double[] { 
				512, 
				512, 
				512, 
				512, 
				512 
			}, 220);
			BCI = new SharpBCIBuilder().EEGDeviceAdapter(adapter).Build();
		}
	}

	void OnDestroy() {
		if (bciType == SharpBCIControllerType.MUSE) {
			if (!museIOProcess.HasExited) {
				museIOProcess.Kill();
				museIOProcess.WaitForExit();
			}
		}
		BCI.Close();
		SharpBCI.Logger.Dispose();
	}
}
