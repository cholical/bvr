using System.Collections.Generic;
using UnityEngine;
using SharpBCI;
using System;

public class ParticleBrainWaveController : MonoBehaviour {

	static Dictionary<EEGDataType, Color> colorsByType = new Dictionary<EEGDataType, Color> { 
		{ EEGDataType.ALPHA_RELATIVE, new Color32(255, 0, 0, 255) },
		{ EEGDataType.BETA_RELATIVE, new Color32(0, 0, 255, 255) },
		{ EEGDataType.GAMMA_RELATIVE, new Color32(0, 230, 0, 255) },
		{ EEGDataType.DELTA_RELATIVE, new Color32(255, 0, 227, 255) },
		{ EEGDataType.THETA_RELATIVE, new Color32(0, 255, 255, 255) },
	};

	public int channel;
	public EEGDataType dataType;
	public double particleFactor = 10;
	public double changeThreshold = 0.1;

	ParticleSystem system;

	double currentPower = 0.5;
	bool powerChanged = true;

	// Use this for initialization
	void Start () {
		system = GetComponent<ParticleSystem>();
		var main = system.main;
		main.startColor = colorsByType[dataType];
		SharpBCIController.BCI.AddRawHandler(dataType, OnEEGData);
	}

	// Update is called once per frame
	void Update() {
		if (powerChanged) { 
			var emit = system.emission;
			var rateMultiplier = (float)(particleFactor * currentPower);
			emit.rateOverTimeMultiplier = rateMultiplier;
		}
	}

	void OnEEGData(EEGEvent evt) {
		if (evt.type != dataType) {
			Debug.LogWarning("Recieved invalid event");
			return;
		}
		powerChanged = Math.Abs(evt.data[channel] - currentPower) >= changeThreshold;
		currentPower = evt.data[channel];
	}
}
