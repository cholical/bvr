using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace SharpBCI
{
	public class KNearestNeighbor
	{

		public List<KeyValuePair<int, double[]>> train_data;
		private double[] test_data;
		private int k_neighbors;

		public KNearestNeighbor (int k)
		{
			train_data = new List<KeyValuePair<int, double[]>>();
			k_neighbors = k;
		}

		public void AddTrainingData(int label, double[] data) {
			AddTrainingData (new KeyValuePair<int, double[]> (label, data));
		}

		public void AddTrainingData(KeyValuePair<int, double[]> data) {
			train_data.Add (data);
		}

		public double Predict (double[] test)
		{

			List<KeyValuePair<int, double>> costs = new List<KeyValuePair<int, double>> ();

			foreach (KeyValuePair<int, double[]> d in this.train_data) {
				costs.Add (new KeyValuePair<int, double> (d.Key, new DynamicTimeWarping (d.Value, test).GetCost ()));
			}
				
			return costs.OrderBy (x => x.Value) //.Take (k_neighbors)  <-- k>1
				.First ().Key; // <-- k==1. which we care about
		}
	}
}

