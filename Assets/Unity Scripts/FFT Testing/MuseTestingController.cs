using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class MuseTestingController : MonoBehaviour {

	public Graph museGraph;
	public Graph bciGraph;
	public EEGDataType type = EEGDataType.ALPHA_ABSOLUTE;

	EEGDeviceAdapter museAdapter;
	SharpBCI.SharpBCI bci;

	EEGEvent lastMuseEvent;
	EEGEvent lastBCIEvent;

	bool museDirty;
	bool bciDirty;

	Queue<Vector2>[] museData = new Queue<Vector2>[4];
	Queue<Vector2>[] bciData = new Queue<Vector2>[4];

	DateTime started;

	// Use this for initialization
	void Start () {
		started = DateTime.UtcNow;

		for (int i = 0; i< 4; i++) {
			museData[i] = new Queue<Vector2>();
			bciData[i] = new Queue<Vector2>();
		}

		SharpBCI.Logger.AddLogOutput(new UnityLogger());
		museAdapter = new RemoteOSCAdapter(5000);
		bci = new SharpBCIBuilder()
			.EEGDeviceAdapter(museAdapter)
			.PipelineFile(System.IO.Path.Combine(Application.streamingAssetsPath, "default_pipeline.json"))
			.Build();

		museAdapter.AddHandler(type, OnMuseEvent);
		bci.AddRawHandler(type, OnBCIEvent);

		museGraph.SetNumSeries(4);
		bciGraph.SetNumSeries(4);
	}

	void OnDestroy() {
		museAdapter.RemoveHandler(type, OnMuseEvent);
		bci.RemoveRawHandler(type, OnBCIEvent);
		bci.Close();
	}


	void OnMuseEvent(EEGEvent evt) {
		//Debug.Log("on muse event " + Thread.CurrentThread.ManagedThreadId);
		for (int i = 0; i < 4; i++) {
			museData[i].Enqueue(new Vector2((float)evt.timestamp.Subtract(started).TotalSeconds, (float)evt.data[i]));
			if (museData[i].Count > 100)
				museData[i].Dequeue();
		}
		museDirty = true;
	}

	void OnBCIEvent(EEGEvent evt) {
		//Debug.Log("On bci event " + Thread.CurrentThread.ManagedThreadId);
		for (int i = 0; i < 4; i++) {
			bciData[i].Enqueue(new Vector2((float)evt.timestamp.Subtract(started).TotalSeconds, (float)evt.data[i]));
			if (bciData[i].Count > 100)
				bciData[i].Dequeue();
		}
		bciDirty = true;
	}

	// Update is called once per frame
	void Update() {
		//Debug.Log("On update " + Thread.CurrentThread.ManagedThreadId);
		//lock (museData) {
		//Debug.Log("Muse data lock obtained");
		if (museDirty) {
			for (int i = 0; i < 4; i++) {
				museGraph.SetData(i, museData[i].ToArray());
			}
			museDirty = false;
		}
		//Debug.Log("Muse data lock released");
		//}
		//lock (bciData) {
			//Debug.Log("BCI data lock obtained");
			if (bciDirty) {
				//Debug.Log("Setting graph data");
				for (int i = 0; i< 4; i++) {
					bciGraph.SetData(i, bciData[i].ToArray());
				}
				bciDirty = false;
			}
			//Debug.Log("BCI data lock released");
		//}
	}
}
