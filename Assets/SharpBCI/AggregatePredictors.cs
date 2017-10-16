using System;
using System.Linq;
using System.Collections.Generic;

namespace SharpBCI {

	/**
	 * A pipeable which aggregates received EEGEvents into an array based on the reported EEGEvent timestamp
	 * and then uses an IPredictor<EEGEvent[]> to train/classify on them
	 */
	public class AggregatePredictionPipeable : Pipeable, IPredictorPipeable {

		public static int ID_PREDICT = 0;

		IPredictor<EEGEvent[]> predictor;
		int trainingId = ID_PREDICT;
		DateTime currentTimeStep;
		EEGEvent[] buffer;
		EEGDataType[] types;
		Dictionary<EEGDataType, int> indexMap;

		public AggregatePredictionPipeable(int channels, int k, double thresholdProb, object[] typeNames) 
			: this(channels, k, thresholdProb, typeNames.Select((x) => (EEGDataType)Enum.Parse(typeof(EEGDataType), (string)x)).ToArray()) {}

		public AggregatePredictionPipeable(int channels, int k, double thresholdProb, EEGDataType[] types) {
			this.types = types;

			buffer = new EEGEvent[types.Length];

			predictor = new AggregateKNNPredictor(channels, k, thresholdProb, types);
			indexMap = new Dictionary<EEGDataType, int>();
			for (int i = 0; i < types.Length; i++) {
				indexMap.Add(types[i], i);
			}
		}

		protected override bool Process(object item) {
			EEGEvent evt = (EEGEvent)item;
			if (!types.Contains(evt.type))
				return true;

			if (evt.timestamp != currentTimeStep) {
				CheckBufferAndPredict();
				currentTimeStep = evt.timestamp;
			}
			
			buffer[indexMap[evt.type]] = evt;

			return true;
		}

		void CheckBufferAndPredict() {
			for (int i = 0; i < buffer.Length; i++) {
				if (buffer[i] == null) return;
			}

			if (trainingId == ID_PREDICT) {
				var prediction = predictor.Predict(buffer);
				if (prediction != -1)
					Add(new TrainedEvent(prediction));
			} else {
				predictor.AddTrainingData(trainingId, buffer);
			}

			buffer = new EEGEvent[types.Length];
		}

		public void StartTraining(int id) {
			if (trainingId != ID_PREDICT)
				throw new InvalidOperationException("Training already started");
			trainingId = id;
		}

		public void StopTraining(int id) {
			if (trainingId == ID_PREDICT)
				throw new InvalidOperationException("No training started");
			trainingId = ID_PREDICT;
		}
	}

	/**
	 * An IPredictor which uses a 3-dimensional loci of points in the form of an array of EEGEvent's to classify EEG data
	 */
	public class AggregateKNNPredictor : IPredictor<EEGEvent[]> {

		readonly Dictionary<EEGDataType, int> bandLookup = new Dictionary<EEGDataType, int>();
		readonly Dictionary<int, List<double[,]>> pointCloud = new Dictionary<int, List<double[,]>>();
		readonly EEGDataType[] bands;
		readonly int channels;
		readonly int k;
		readonly double thresholdProb;

		public AggregateKNNPredictor(int channels, int k, double thresholdProb, EEGDataType[] bands) {
			this.channels = channels;
			this.bands = bands;
			this.k = k;
            this.thresholdProb = thresholdProb;

			for (int i = 0; i < bands.Length; i++) {
				bandLookup.Add(bands[i], i);
			}
		}

		public int Predict(EEGEvent[] events) {
			var bandSpacePoint = TransformToBandSpace(events);

			var distances = new List<KeyValuePair<int, double>>();

			// O(N) * O(|channels|) * O(|bands|) performance
			foreach (var pair in pointCloud) {
				foreach (var point in pair.Value) { 
					double dist = Distance(bandSpacePoint, point);
					distances.Add(new KeyValuePair<int, double>(pair.Key, dist));
				}
			}

			if (distances.Count == 0)
				return -1;

			var nearestNeighbors = distances.OrderBy((x) => x.Value).Take(k);

			// use a plurality voting system weighted by distance from us
			double voteSum = 0;
			Dictionary<int, double> votes = new Dictionary<int, double>();
			foreach (var neighbor in nearestNeighbors) {
				if (!votes.ContainsKey(neighbor.Key))
					votes.Add(neighbor.Key, 0);
				var vote = 1.0 / neighbor.Value;
				votes[neighbor.Key] += vote;
				voteSum += vote;
			}

			var winner = votes.OrderBy((x) => x.Value).First();
			return winner.Value / voteSum > thresholdProb ? winner.Key : -1;
		}

		public void AddTrainingData(int id, EEGEvent[] events) {
			if (!pointCloud.ContainsKey(id))
				pointCloud.Add(id, new List<double[,]>());
			pointCloud[id].Add(TransformToBandSpace(events));
		}

		double[,] TransformToBandSpace(EEGEvent[] events) {
			// transform to a set of points in band-space
			double[,] points = new double[channels, bands.Length];
			foreach (EEGEvent evt in events) {
				var bIdx = bandLookup[evt.type];
				for (int cIdx = 0; cIdx < evt.data.Length; cIdx++) {
					points[cIdx, bIdx] = evt.data[cIdx];
				}
			}
			return points;
		}

		double Distance(double[,] a, double[,] b) {
			double dist = 0;
			for (int cIdx = 0; cIdx < channels; cIdx++) {
				double bSum = 0;
				for (int bIdx = 0; bIdx < bands.Length; bIdx++) {
					bSum += Math.Abs(a[cIdx, bIdx] - b[cIdx, bIdx]);
				}
				dist += bSum / bands.Length;
			}
			dist /= channels;
			return dist;
		}
	}
}