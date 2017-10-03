using UnityEngine;
using UnityEngine.UI;
using SharpBCI;

public class FFTGraphController : MonoBehaviour {

	public int channel = 0;

	public Text minX;
	public Text maxX;

	public Text maxY;
	public Text minY;

	public int updateRate = 1;

	public FFTGraph realGraph;
	//public FFTGraph complexGraph;

	int lastUpdate;

	EEGEvent currentEvent;
	bool isDirty = false;

	// Use this for initialization
	void Start() {
		realGraph.showComplexData = false;
		//complexGraph.showComplexData = true; 
		SharpBCIController.BCI.AddRawHandler(EEGDataType.FFT_RAW, OnEEGData);
	}

	void OnDestroy() {
		SharpBCIController.BCI.RemoveRawHandler(EEGDataType.FFT_RAW, OnEEGData);
	}

	void Update() {
		if (isDirty) {
			ProcessData();
		}
	}

	void ProcessData() {
		realGraph.SetData(currentEvent.data);
		//complexGraph.SetData(currentEvent.data);
	}

	void OnEEGData(EEGEvent evt) {
		if (((int) evt.extra) != channel)
			return;
		if (lastUpdate % updateRate == 0) {
			currentEvent = evt;
			isDirty = true;
		}
		lastUpdate++;
	}
}
