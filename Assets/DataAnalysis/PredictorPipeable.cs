using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SharpBCI
{
	public class PredictorPipeable : Pipeable
	{
		public const int TEST_ID = 0;
		public const int DEFAULT_VALUE = -1;

		private const int MODE_WINDOW_SIZE = 20;

		private NearestNeighborPredictor predictor;
		private int bufferSize;
		private EEGDataType type = EEGDataType.FFT_RAW;
		private List<int> modeWindow = new List<int> ();
		private int training;

		public PredictorPipeable (int bufferSize, int channels)
		{
			this.predictor = new KNearestNeighbor ();

			this.bufferSize = bufferSize;

			training = TEST_ID;



			AddTrainingData(DEFAULT_VALUE, new double[bufferSize]);

		}

		DynamicallyAveragedData trainingData = new DynamicallyAveragedData();
		public void StartTraining (int id)
		{
			if(training != TEST_ID) 
				throw new ArgumentException ("Called StartTraining again without a Complementary StopTraining");
			training = id;

		}

		public void StopTraining (int id)
		{
			if(training == TEST_ID) 
				throw new ArgumentException ("Called StopTraining without an open StartTraining");
			AddTrainingData (training, trainingData.Data);
			trainingData.Clear ();
			training = TEST_ID;
		}




		protected override bool Process (object item)
		{
			//Ensure the object is the EEGDataType we want (FFT_RAW).
			EEGEvent evt = (EEGEvent) item;

			if (evt.type != type)
				return true;


			if (training != TEST_ID) { //Training
				trainingData.AddData (evt.data);
			} else { //Testing
				int prediction = predictor.Predict (evt.data);

				modeWindow.Add (prediction);

				if( modeWindow.Count == MODE_WINDOW_SIZE) {
					
					var groups = modeWindow.GroupBy(v => v);
					int maxCount = groups.Max(g => g.Count());
					int outgoingPrediction = groups.First(g => g.Count() == maxCount).Key;

					Logger.Log(string.Format("Predicted: {0}", outgoingPrediction));
					modeWindow.Clear ();
					Add (new TrainedEvent (outgoingPrediction));
				}
			}
				
			return true;
		}
			
		private void AddTrainingData (int label, double[] data)
		{
			predictor.AddTrainingData (label, data);
		}

	}

	public class DynamicallyAveragedData {
		private int count;
		public double[] Data;

		public DynamicallyAveragedData() {
			this.count = 0;
		}

		public void AddData(double[] newData) {
			if (Data == null)
				Data = newData;
			if (newData.Length != Data.Length)
				throw new ArgumentException ("Bad Training Data");
			count++;
			for (int i = 0; i < Data.Length; i++) {
				double oldPart = ((double)(count - 1) / (double) count) * Data [i];
				double newPart = ((double)(1) / (double) count) * newData [i];
				Data [i] = oldPart + newPart;
			}
		}

		public void Clear() {
			count = 0;
			Data = null;
		}

	}

}

