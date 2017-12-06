using SharpBCI;
using UnityEngine;
using System.Diagnostics;

public enum SharpBCIControllerType {
	MUSE,
	TONE_GENERATOR,
	TWO_TONE_GENERATOR,
	CSV_READER
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
	public static EEGDeviceAdapter adapter;

	public SharpBCIControllerType bciType;

	public EEGDataType dataType;

	public string CSVReadFilePath;

	static SharpBCIController _inst;

	Process museIOProcess;

	bool isTheHighlander = false;

	// Use this for initialization
	void Awake() {
		if (_inst != null) {
			Destroy (gameObject);
			return;
		}
		_inst = this;
		DontDestroyOnLoad (gameObject);
		isTheHighlander = true;

		// FileLogger requires actual pathnames not Unity
		string logName = System.IO.Path.Combine(Application.persistentDataPath.Replace('/', System.IO.Path.DirectorySeparatorChar), LOG_NAME);
		UnityEngine.Debug.Log("Writing sharpBCI log to: " + logName);
		// configure logging
		SharpBCI.Logger.AddLogOutput (new UnityLogger ());
		//SharpBCI.Logger.AddLogOutput(new FileLogger(logName));

		//EEGDeviceAdapter adapter;
		if (bciType == SharpBCIControllerType.MUSE) {
			// start Muse-IO
			try {
				museIOProcess = new Process ();
				museIOProcess.StartInfo.FileName = System.IO.Path.Combine (Application.streamingAssetsPath, "MuseIO", "muse-io.exe");
				// default is osc.tcp://localhost:5000, but we expect udp
				museIOProcess.StartInfo.Arguments = "--osc osc.udp://localhost:5000";
				museIOProcess.StartInfo.CreateNoWindow = true;
				museIOProcess.StartInfo.UseShellExecute = false;
				museIOProcess.Start ();
				museIOProcess.PriorityClass = ProcessPriorityClass.RealTime;
			} catch (System.Exception e) {
				UnityEngine.Debug.LogError ("Could not open muse-io:");
				UnityEngine.Debug.LogException (e);
			}

			adapter = new RemoteOSCAdapter (OSC_DATA_PORT);
		} else if (bciType == SharpBCIControllerType.TONE_GENERATOR) {
			adapter = new DummyAdapter (new DummyAdapterSignal (new double[] { 
				// alpha
				10, 
				// beta
				24, 
				// gamma
				40, 
				// delta
				2, 
				// theta
				6,
				// simulate AC interference
				60,
			}, new double[] {
				512,
				512,
				512,
				512,
				512,
				512
			}), 220, 2);
		} else if (bciType == SharpBCIControllerType.TWO_TONE_GENERATOR) {
			var signals = new DummyAdapterSignal[] { 
				new DummyAdapterSignal (new double[] { 
					// alpha
					10, 
					// beta
					24, 
					// gamma
					40, 
					// delta
					2, 
					// theta
					6,
				}, new double[] {
					512,
					0,
					0,
					0,
					0
				}),
				new DummyAdapterSignal (new double[] { 
					// alpha
					10, 
					// beta
					24, 
					// gamma
					40, 
					// delta
					2, 
					// theta
					6,
				}, new double[] {
					0,
					512,
					0,
					0,
					0
				})
			};
			adapter = new InstrumentedDummyAdapter (signals, 220, 2);
		} else if (bciType == SharpBCIControllerType.CSV_READER) {
			double sampleRate = 220;
			adapter = new CSVReadAdapter (CSVReadFilePath, sampleRate);
		} else {
			throw new System.Exception("Invalid bciType");
		}

		BCI = new SharpBCIBuilder()
			.EEGDeviceAdapter(adapter)
			.PipelineFile(System.IO.Path.Combine(Application.streamingAssetsPath, "default_pipeline.json"))
			.Build();

		if (bciType != SharpBCIControllerType.CSV_READER) {
			BCI.LogRawData(dataType);
		}
	}

	void OnDestroy() {
		if (!isTheHighlander)
			return;
		
		if (bciType == SharpBCIControllerType.MUSE) {
			if (museIOProcess != null && !museIOProcess.HasExited) {
				museIOProcess.Kill();
				museIOProcess.WaitForExit();
			}
		}
			
		BCI.Close();
		SharpBCI.Logger.Dispose();
	}
}
