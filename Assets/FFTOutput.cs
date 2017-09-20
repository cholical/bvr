using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lomont;

public class FFTOutput : MonoBehaviour {

	int channel = 0;
	int windowSize = 128;
	Queue<double> sampleWindow = new Queue<double>();
	LomontFFT fft;
	ScrollingGraph graph;

	// Use this for initialization
	//void Start () {
	//	fft = new LomontFFT();
	//	DeviceController.Device.AddHandler(SharpBCI.EEGDataType.EEG, DoFFT);
	//}

	void DoFFT(SharpBCI.EEGEvent evt) {
		double d = evt.data[channel];
		if (sampleWindow.Count/2 >= windowSize){
			sampleWindow.Dequeue();
			sampleWindow.Dequeue();
		}
		sampleWindow.Enqueue(d);
		sampleWindow.Enqueue(0);

		if (sampleWindow.Count/2 == windowSize) {
			double[] data = sampleWindow.ToArray();			fft.FFT(data, true);
		}
	}
}
