using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SharpBCI;

//[ExecuteInEditMode]
public class ScrollingGraph : Graphic {

	public float windowSize = 30;
	public float lineThickness = 1;
	public Color[] lineColors = new Color[] {
		Color.blue,
		Color.red,
		Color.green,
		Color.yellow,
		Color.black,
	};

	private Queue<EEGEvent> values = new Queue<EEGEvent>();

	private DateTime minX;
	private DateTime maxX;

	private float minY;
	private float maxY;

	private bool valuesDirty = false;

	public void AppendValue(EEGEvent evt) {
		values.Enqueue(evt);
		SetVerticesDirty();
		// flag we need to trim values and calculate extends
		valuesDirty = true;
	}

	protected override void OnPopulateMesh(VertexHelper vh) {
		if (values.Count < 2)
			return;

		vh.Clear();

		if (valuesDirty) {
			TrimValues();
			CalcExtents();
			valuesDirty = false;
		}

		float deltaTime = (float) maxX.Subtract(minX).TotalSeconds;

		float sizeX = rectTransform.rect.width;
		float sizeY = rectTransform.rect.height;
		Vector2 offset = new Vector2(rectTransform.pivot.x * sizeX, rectTransform.pivot.y * sizeY);

		Vector2[] prevValues = null;
		int i = 0;
		foreach (EEGEvent evt in values) {
			float[] arr = evt.data;
			if (prevValues == null)
				prevValues = new Vector2[arr.Length];

			float time = (float) evt.timestamp.Subtract(minX).TotalSeconds;
			float scaledTime = Rescale(time, 0, deltaTime, 0f, sizeX);
			for (int j = 0; j < arr.Length; j++) {
				var v = arr[j];
				if (float.IsNaN(v))
					continue;

				Vector2 curr = new Vector2(scaledTime, Rescale(v, minY, maxY, 0f, sizeY));
				curr -= offset;
				if (i > 0) {
					Vector2 prev = prevValues[j];
					var v1 = prev + new Vector2(0, -lineThickness / 2);
					var v2 = prev + new Vector2(0, +lineThickness / 2);
					var v3 = curr + new Vector2(0, +lineThickness / 2);
					var v4 = curr + new Vector2(0, -lineThickness / 2);

					MakeQuad(vh, new Vector2[] { v1, v2, v3, v4 }, lineColors[j]);
				}
				prevValues[j] = curr;
			}
			i++;
		}
	}

	private float Rescale(float x, float oldMin, float oldMax, float newMin, float newMax) {
		return ((newMax - newMin) * (x - oldMin)) / (oldMax - oldMin) + newMin;
	}

	private void TrimValues() {
		DateTime windowEnd = DateTime.UtcNow.AddSeconds(-windowSize);
		while (values.Count > 0 && values.Peek().timestamp < windowEnd) {
			values.Dequeue();
		}
	}

	private void CalcExtents() {
		minY = float.PositiveInfinity;
		maxY = float.NegativeInfinity;

		minX = values.Peek().timestamp;
		maxX = values.Peek().timestamp;

		foreach (var pt in values) {
			if (pt.timestamp < minX) {
				minX = pt.timestamp;
			}
			if (pt.timestamp > maxX) {
				maxX = pt.timestamp;
			}

			foreach (var value in pt.data) {
				if (float.IsNaN(value))
					continue;

				if (value < minY) {
					minY = value;
				} 
				if (value > maxY) {
					maxY = value;
				}	
			}
		}
	}

	private void MakeQuad(VertexHelper vh, Vector2[] vertices, Color c) {
		UIVertex[] verts = new UIVertex[vertices.Length];
		for (int i = 0; i < vertices.Length; i++) {
			var vert = UIVertex.simpleVert;
			vert.color = c;
			vert.position = vertices[i];
			verts[i] = vert;
		}
		vh.AddUIVertexQuad(verts);
	}

//	private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
//		Vector3 dir = point - pivot; // get point direction relative to pivot
//		dir = Quaternion.Euler(angles)*dir; // rotate it
//		point = dir + pivot; // calculate rotated point
//		return point; // return it
//	}
}
