using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpBCI
{
	public class KNearestNeighborPipeable : Pipeable
	{
		private KNearestNeighbor knn;
		private int bufferSize;
		private int channels;
		private Queue<double[]>[] channelList;
		private List<double> buffer;
		private int training;

		public KNearestNeighborPipeable (int bufferSize, int channels)
		{
			this.knn = new KNearestNeighbor (1);
			this.bufferSize = bufferSize;
			buffer = new List<double> ();


			this.channels = channels;
			channelList = new Queue<double[]>[channels];
			for (int i = 0; i< channels; i++) {
				channelList[i] = new Queue<double[]>();
			}

			training = 0;
			AddTrainingData(-1, new double[bufferSize]);

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
			//Ensure the object is the EEGDataType we want (FFT_RAW).
			EEGEvent evt = (EEGEvent) item;

			if (evt.type != EEGDataType.FFT_RAW)
				return true;


//			//Add the data to a queue associated with the channel.
//			int evtChannel = (int) evt.extra;
//			channelList [evtChannel].Enqueue (evt.data);
//
//			double[][] matrixToAverage = new double[channels][];
//
//			foreach (Queue<double[]> q in channelList)
//				if (q.Peak () == null) {
//					return true;
//				} else {
//					matrixToAverage[
//						
//				
//			double[] toKNN = new double[channelList [0].Count];
//			foreach (int


//			buffer.Add ((double) item);

//			if (buffer.Count == bufferSize) {
				if (training != 0) {
					AddTrainingData (training, evt.data);
				} else {
					int prediction = knn.Predict (evt.data);
				Logger.Log(string.Format("Predicted: {0}", prediction));
				Add (new TrainedEvent (prediction));
				}
//				buffer.Clear();
//			}
			return true;
		}
			
		private void AddTrainingData (int label, double[] data)
		{
			knn.AddTrainingData (label, data);
		}
			
	}
}

