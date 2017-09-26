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


<<<<<<< HEAD
		public KNearestNeighborPipeable(int bufferSize) {
			this.knn = new KNearestNeighbor (1);
			this.bufferSize = bufferSize;
			buffer = new List<double>();
			training = 0;

		}

		public void StartTraining(int id) {
			training = id;
		}

		public void StopTraining(int id) {
=======
		public KNearestNeighborPipeable (int bufferSize)
		{
			this.knn = new KNearestNeighbor (1);
			this.bufferSize = bufferSize;
			buffer = new List<double> ();
			training = 0;
			AddTrainingData(-1, new double[bufferSize]);

		}

		public void StartTraining (int id)
		{
			training = id;
		}

		public void StopTraining (int id)
		{
>>>>>>> master
			training = 0;
		}

		protected override bool Process (object item)
		{
			if (!(item is double)) {
				return false;
			}

<<<<<<< HEAD
			buffer.Add ((double) item);

			if (buffer.Count == bufferSize) {
				lock(training) {
					if(training != 0) {
						AddTrainingData(training, buffer.ToArray());
					}else {
						Add (knn.Predict (buffer.ToArray ()));
					}
			}
			}
=======
			buffer.Add ((double)item);

			if (buffer.Count == bufferSize) {
				if (training != 0) {
					AddTrainingData (training, buffer.ToArray ());
				} else {
					Add (new TrainedEvent (knn.Predict (buffer.ToArray ())));
				}
				buffer.Clear();
>>>>>>> master
			}
			return true;
		}

<<<<<<< HEAD
		private void AddTrainingData(int label, double[] data) {
=======
		private void AddTrainingData (int label, double[] data)
		{
>>>>>>> master
			knn.AddTrainingData (label, data);
		}
			
	}
}

