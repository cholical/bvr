using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class RelaxController : MonoBehaviour {

	public int side = 0;
	//public Color startColor;
	//public Color endColor;

	public EEGDataType EEGType;

	//public Renderer targetRenderer;

	public Transform fireHeight;

	float fireMin = 0f;
	float fireMax = 5.5f;

	double sessionMinPower = 0f;
	double sessionMaxPower = 0f;
	double currentPower = 0f;

	// Use this for initialization
	void Start () {
		SharpBCIController.BCI.AddRawHandler(EEGType, OnEEGData);
	}

	// Update is called once per frame
	void OnDestroy() {
		SharpBCIController.BCI.RemoveRawHandler(EEGType, OnEEGData);
	}

	void Update() {
		float t = Mathf.InverseLerp((float) sessionMinPower, (float) sessionMaxPower, (float) currentPower);
		Vector3 scale = fireHeight.localScale;
		scale.y = Mathf.Lerp(fireMax, fireMin, t);
		fireHeight.localScale = scale;
		//float normed = Mathf.Lerp(0, 1, t);
		//Color c = Color.Lerp(startColor, endColor, t);
		//targetRenderer.material.color = c;
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
		for (int i = min; i < max; i++) {
			double v = evt.data[i];
			if (double.IsNaN(v))
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
