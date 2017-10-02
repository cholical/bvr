using System;
using UnityEngine;
using UnityEngine.UI;

public class FFTGraph : Graphic {
	
	public float lineThickness = 1;
	public Color lineColor = Color.red;
	public int maxX = 10;
	public bool showComplexData;

	double[] data;

	int minX = 0;

	double minY;
	double maxY;

	bool valuesDirty = false;

	public void SetData(double[] data) {
		this.data = data;
		SetVerticesDirty();
		// flag we need to trim values and calculate extends
		valuesDirty = true;
	}


	protected override void OnPopulateMesh(VertexHelper vh) {
		vh.Clear();

		if (data == null || data.Length < 2)
			return;

		if (valuesDirty) {
			CalcExtents();
			valuesDirty = false;
		}

		int deltaTime = maxX - minX;

		double sizeX = rectTransform.rect.width;
		double sizeY = rectTransform.rect.height;
		Vector2 offset = new Vector2((float)(rectTransform.pivot.x * sizeX), (float)(rectTransform.pivot.y * sizeY));

		Vector2 prev = Vector3.zero;

		for (int i = 0; i < maxX; i++) {
			double v = Math.Abs(data[i]);
			var scaledX = Rescale(i, 0, deltaTime, 0, sizeX);
			Vector2 curr = new Vector2((float)scaledX, (float)Rescale(v, minY, maxY, 0f, sizeY));
			curr -= offset;
			if (i > 1) {
				var v1 = prev + new Vector2(0, -lineThickness / 2);
				var v2 = prev + new Vector2(0, +lineThickness / 2);
				var v3 = curr + new Vector2(0, +lineThickness / 2);
				var v4 = curr + new Vector2(0, -lineThickness / 2);
				MakeQuad(vh, new Vector2[] { v1, v2, v3, v4 }, lineColor);
			}
			prev = curr;
		}
	}

	double Rescale(double x, double oldMin, double oldMax, double newMin, double newMax) {
		return ((newMax - newMin) * (x - oldMin)) / (oldMax - oldMin) + newMin;
	}

	void CalcExtents() {
		//minY = float.PositiveInfinity;
		//maxY = float.NegativeInfinity;

		//minX = 0;
		if (maxX <= 0 || maxX > data.Length)
			maxX = data.Length;

		for (int i = 0; i<maxX; i++) {
			var v = data[i];
			if (double.IsNaN(v)) continue;
			if (v < minY) minY = v;
			if (v > maxY) maxY = v;
		}
	}

	void MakeQuad(VertexHelper vh, Vector2[] vertices, Color c) {
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
