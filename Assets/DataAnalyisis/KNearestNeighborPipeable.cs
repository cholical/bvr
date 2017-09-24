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
		private int training;


		public KNearestNeighborPipeable (int bufferSize)
		{
			this.knn = new KNearestNeighbor (1);
			this.bufferSize = bufferSize;
			buffer = new List<double> ();
			training = 0;

		}

		public void StartTraining (int id)
		{
			training = id;
		}

		public void StopTraining (int id)
		{
			training = 0;
		}

		protected override bool Process (object item)
		{
			if (!(item is double)) {
				return false;
			}

			buffer.Add ((double)item);

			if (buffer.Count == bufferSize) {
				if (training != 0) {
					AddTrainingData (training, buffer.ToArray ());
				} else {
					Add (new TrainedEvent (knn.Predict (buffer.ToArray ())));
				}

			}
			return true;
		}

		private void AddTrainingData (int label, double[] data)
		{
			knn.AddTrainingData (label, data);
		}
			
	}
}

