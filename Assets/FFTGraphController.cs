using System;

using UnityEngine;
using UnityEngine.UI;
using SharpBCI;

public class FFTGraphController : MonoBehaviour {

	//public int channel = 0;

	public EEGDataType dataType = EEGDataType.FFT_RAW;

	public int bandStart = 0;
	public int bandEnd = 0;

	public int updateRate = 1;

	public Graph realGraph;

	int lastUpdate;

	EEGEvent[] eventBuffer;
	bool isDirty = false;

	double sampleRate;
	// Use this for initialization
	void Start() {
		eventBuffer = new EEGEvent[SharpBCIController.BCI.channels];
		sampleRate = SharpBCIController.BCI.sampleRate;
		realGraph.xFormatter = (x) => string.Format("{0:0} Hz", x);
		realGraph.SetNumSeries(eventBuffer.Length);
		SharpBCIController.BCI.AddRawHandler(dataType, OnEEGData);
	}

	void OnDestroy() {
		SharpBCIController.BCI.RemoveRawHandler(dataType, OnEEGData);
	}

	void Update() {
		if (isDirty) {
			ProcessData();
		}
	}

	void ProcessData() {
		for (int i = 0; i < eventBuffer.Length; i++) {
			var currentEvent = eventBuffer[i];
			if (currentEvent == null) continue;
			if (bandStart < 0)
				bandStart = 0;

			if (bandEnd >= currentEvent.data.Length)
				bandEnd = currentEvent.data.Length - 1;

			if (bandEnd < bandStart)
				bandEnd = bandStart;

			var graphData = new Vector2[bandEnd - bandStart + 1];

			var N = currentEvent.data.Length;
			//Debug.Log(string.Format("FFT N={0}", currentEvent.data.Length));
			var increment = (sampleRate / 2.0) / (N - 1);
			for (var j = bandStart; j <= bandEnd; j++) {
				var freq = j * increment;
				graphData[j - bandStart] = new Vector2((float)freq, (float)currentEvent.data[j]);
			}
			realGraph.SetData(i, graphData);
		}
	}

	void OnEEGData(EEGEvent evt) {
		if (lastUpdate % updateRate == 0) {
			eventBuffer[(int)evt.extra] = evt;
			isDirty = true;
		}
		lastUpdate++;
	}
}
