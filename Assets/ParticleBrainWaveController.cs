using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class ParticleBrainWaveController : MonoBehaviour {

	public int channel;
	public EEGDataType dataType;
	public double particleFactor = 10;

	ParticleSystem system;

	double currentPower;

	// Use this for initialization
	void Start () {
		SharpBCIController.BCI.AddRawHandler(dataType, OnEEGData);
	}

	// Update is called once per frame
	void Update() {
		var emit = system.emission;
		emit.rateOverTimeMultiplier = (float)(particleFactor * currentPower);	}

	void OnEEGData(EEGEvent evt) {		if (evt.type != dataType) {
			Debug.LogWarning("Recieved invalid event");
		}
		currentPower = evt.data[channel];
	}
}
