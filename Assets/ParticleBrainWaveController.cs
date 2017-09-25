using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class ParticleBrainWaveController : MonoBehaviour {

	static Dictionary<EEGDataType, Color> colorsByType = new Dictionary<EEGDataType, Color> { 
		{ EEGDataType.ALPHA_RELATIVE, new Color32(255, 0, 0, 200) },
		{ EEGDataType.BETA_RELATIVE, new Color32(0, 0, 255, 200) },
		{ EEGDataType.GAMMA_RELATIVE, new Color32(0, 230, 0, 200) },
		{ EEGDataType.DELTA_RELATIVE, new Color32(255, 0, 227, 200) },
		{ EEGDataType.THETA_RELATIVE, new Color32(0, 255, 255, 200) },
	};

	public int channel;
	public EEGDataType dataType;
	public double particleFactor = 10;

	ParticleSystem system;

	double currentPower;

	// Use this for initialization
	void Start () {
		system = GetComponent<ParticleSystem>();
		var main = system.main;
		main.startColor = colorsByType[dataType];
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
