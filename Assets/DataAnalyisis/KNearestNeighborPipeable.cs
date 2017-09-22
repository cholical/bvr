using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SharpBCI
{
	public class KNearestNeighborPipeable : Pipeable
	{
		private KNearestNeighbor knn;
		private int bufferSize;
		private List<double> buffer;


		public KNearestNeighborPipeable(int bufferSize) {
			this.knn = new KNearestNeighbor (1);
			this.bufferSize = bufferSize;
			buffer = new List<double>();

		}

		public void AddTrainingData(int label, double[] data) {
			knn.AddTrainingData (label, data);
		}

		protected override bool Process (object item)
		{
			if (!(item is double)) {
				return false;
			}

			buffer.Add ((double) item);

			if (buffer.Count == bufferSize) {
				this.Add (knn.Predict (buffer.ToArray ()));
			}
			return true;
		}
			
	}
}

