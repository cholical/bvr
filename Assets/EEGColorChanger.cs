using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class EEGColorChanger : MonoBehaviour {

	public int side = 0;
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

		int max = evt.data.Length;
		int min = 0;
		if (side == -1) {
			max = evt.data.Length / 2;
		} else if (side == 1) {
			min = evt.data.Length / 2;
		}

		int n = 0;
		for (int i = 0; i < max; i++) {
			float v = evt.data[i];
			if (float.IsNaN(v))
				continue;
			currentPower += v;
			n++;
		}

		if (n > 0)
			currentPower /= n;
		
		if (currentPower < sessionMinPower) {
			sessionMinPower = currentPower;
		}

		if (currentPower > sessionMaxPower) {
			sessionMaxPower = currentPower;
		}
	}
}
