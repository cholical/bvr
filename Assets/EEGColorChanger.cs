using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class EEGColorChanger : MonoBehaviour {

	public Color startColor;
	public Color endColor;

	public EEGDataType EEGType;

	public Renderer targetRenderer;

	private float sessionMinPower = 0f;
	private float sessionMaxPower = 0f;
	private float currentPower = 0f;

	// Use this for initialization
	void Start () {
		DeviceController.Device.AddHandler(EEGType, OnEEGData);
	}
	
	// Update is called once per frame
	void OnDestroy() {
		DeviceController.Device.RemoveHandler(EEGType, OnEEGData);
	}

	void Update() {
		float t = Mathf.InverseLerp(sessionMinPower, sessionMaxPower, currentPower);
		Color c = Color.Lerp(startColor, endColor, t);
		targetRenderer.material.color = c;
	}

	void OnEEGData(EEGEvent evt) {
		currentPower = 0;
		int i = 0;

		foreach (float v in evt.data) {
			if (float.IsNaN(v))
				continue;
			currentPower += v;
			i++;
		}

		if (i > 0)
			currentPower /= i;
		
		if (currentPower < sessionMinPower) {
			sessionMinPower = currentPower;
		}

		if (currentPower > sessionMaxPower) {
			sessionMaxPower = currentPower;
		}
	}
}
