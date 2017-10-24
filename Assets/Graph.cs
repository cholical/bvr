using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;


public class Graph : MonoBehaviour {

	public delegate string AxisFormatter(double x);

	public Image barPrefab;
	public Text labelPrefab;

	public float barThickness = 1;

	public Color[] barColors = new Color[] {
		Color.red,
		Color.blue,
		Color.green,
		Color.yellow,
	};

	public AxisFormatter xFormatter = (x) => string.Format("{0:0}", x);
	public AxisFormatter yFormatter = (y) => string.Format("{0:0}", y);

	readonly List<Vector2[]> dataMap = new List<Vector2[]>();

	float minX;
	float maxX;

	float minY;
	float maxY;

	bool valuesDirty;

	RectTransform rectTransform;

	Text minYLabel;
	Text maxYLabel;

	List<List<RectTransform>> barsMap = new List<List<RectTransform>>();
	List<Text> labelObjs = new List<Text>();

	public void SetNumSeries(int n) {
		for (int i = 0; i < n; i++) {
			dataMap.Add(new Vector2[0]);
			barsMap.Add(new List<RectTransform>());
		}
	}

	public void SetData(int key, Vector2[] data) {
		if (key < 0 || key > dataMap.Count)
			throw new ArgumentOutOfRangeException();
		
		dataMap[key] = data;
		// flag we need to trim values and calculate extends
		valuesDirty = true;
	}

	void Start() {
		rectTransform = (RectTransform)transform;
		for (var i = 0; i < rectTransform.childCount; i++) {
			Destroy(rectTransform.GetChild(i).gameObject);
		}
	}

	void Update() {
		if (valuesDirty)
			UpdateData();
	}

	void UpdateData() {
		valuesDirty = false;

		CalcExtents();

		float sizeX = rectTransform.rect.width;
		float sizeY = rectTransform.rect.height;

		Vector2 offset = new Vector2(rectTransform.pivot.x * sizeX, rectTransform.pivot.y * sizeY);

		//Debug.Log(string.Format("Graph extends: {0}, {1}, {2}, {3}", minX, maxX, minY, maxY));

		// create bars based on dataMap
		int colorIdx = 0;
		float spacing = 1.5f * barThickness;
		float barOffset = -spacing * ((dataMap.Count) / 2.0f);

		float labelOffset = labelPrefab.rectTransform.rect.width;

		for (int key = 0; key < dataMap.Count; key++) {
			var arr = dataMap[key];
			if (arr.Length < 2) continue;

			var barsList = barsMap[key];
			while (barsList.Count < arr.Length) {
				var clone = Instantiate(barPrefab, rectTransform);
				clone.color = barColors[colorIdx];
				clone.name = string.Format("Graph Bar Series {0}, Idx {1}", key, barsList.Count);
				barsList.Add(clone.rectTransform);
			}

			for (int i = 0; i < arr.Length; i++) {
				var v = arr[i];

				var vX = v.x;
				var vY = v.y;

				if (!IsValidPoint(vX) || !IsValidPoint(vY)) continue;
					
				var scaledX = Rescale(v.x, minX, maxX, labelOffset, sizeX) - offset.x + barOffset;
				var scaledY = Rescale(v.y, minY, maxY, 0f, sizeY - labelPrefab.rectTransform.rect.height) - offset.y;

				//Debug.Log(string.Format("v.y {0}, ScaledY {1}", v.y, scaledY));
				var bar = barsList[i];

				var newPos = new Vector3(scaledX, scaledY, bar.localPosition.z);
				bar.localPosition = newPos;
				bar.sizeDelta = new Vector2(barThickness, rectTransform.rect.height + scaledY);
			}

			barOffset += spacing;
			colorIdx++;
		}

		// yAxis labels
		if (minYLabel == null) {
			minYLabel = Instantiate(labelPrefab, rectTransform);
			minYLabel.name = "Graph Y-Label Min";
			minYLabel.alignment = TextAnchor.MiddleRight;
			var yPos = -offset.y + 2 * labelPrefab.rectTransform.rect.height;
			minYLabel.rectTransform.localPosition = new Vector3(-offset.x, yPos, minYLabel.rectTransform.localPosition.z);
		}
		minYLabel.text = yFormatter(minY);

		if (maxYLabel == null) {
			maxYLabel = Instantiate(labelPrefab, rectTransform);
			maxYLabel.name = "Graph Y-Label Max";
			maxYLabel.alignment = TextAnchor.UpperRight;
			maxYLabel.rectTransform.localPosition = new Vector3(-offset.x, 0, maxYLabel.rectTransform.localPosition.z);
		}
		maxYLabel.text = yFormatter(maxY);

		// now make labels for xAxis
		int maxLabels = (int)Math.Ceiling(sizeX / labelPrefab.rectTransform.rect.width);
		int labelStep = Math.Max(1, (int)Math.Ceiling((maxX - minX) / maxLabels));
		var x = (int) Math.Round(minX);
		int j = 0;

		//Debug.Log(string.Format("Max Labels {0}, Label Step {1}", maxLabels, labelStep));

		if (!IsValidPoint(minX) || !IsValidPoint(maxX) || maxX < minX || labelStep <= 0) {
			Debug.LogWarning("Graph ended up in invalid state");
			return;
		}
		
		while (x < maxX) {
			var scaledX = Rescale(x, minX, maxX, labelOffset, sizeX) - offset.x;
			if (j == labelObjs.Count) {
				var clone = Instantiate(labelPrefab, rectTransform);
				clone.name = string.Format("Graph X-Label {0}", labelObjs.Count);
				clone.alignment = TextAnchor.UpperCenter;
				labelObjs.Add(clone);
			}
			var label = labelObjs[j++];
			label.gameObject.SetActive(true);
			label.rectTransform.localPosition = new Vector3(scaledX, -rectTransform.rect.height, label.rectTransform.localPosition.z);
			label.text = xFormatter(x);
			x += labelStep;
		}

		// hide unused labels
		while (j < labelObjs.Count) {
			labelObjs[j++].gameObject.SetActive(false);
		}
	}

	float Rescale(float x, float oldMin, float oldMax, float newMin, float newMax) {
		if (Mathf.Abs(oldMax - oldMin) < 1e-5)
			return ((newMax - newMin) * 0.5f) + newMin;
		return ((newMax - newMin) * (x - oldMin)) / (oldMax - oldMin) + newMin;
	}

	void CalcExtents() {
		minX = float.PositiveInfinity;
		maxX = float.NegativeInfinity;
		minY = float.PositiveInfinity;
		maxY = float.NegativeInfinity;
		foreach (var key in dataMap) {
			foreach (var v in key) {
				if (!IsValidPoint(v.x) || !IsValidPoint(v.y)) continue;
				if (v.x < minX)
					minX = v.x;
				if (v.x > maxX)
					maxX = v.x;
				if (v.y < minY)
					minY = v.y;
				if (v.y > maxY)
					maxY = v.y;
			}
		}
	}

	bool IsValidPoint(float x) {
		return !(float.IsNaN(x) || float.IsInfinity(x));
	}
}
