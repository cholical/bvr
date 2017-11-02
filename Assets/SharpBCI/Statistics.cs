using System;
using System.Collections.Generic;

namespace SharpBCI {

	public class OnlineVariance {
		uint n = 0;
		double _mean = 0.0;
		double _mean2 = 0.0;

		double _var = double.NaN;

		double mean { get { return _mean; } }
		double var { get { return _var; } }

		public void Update(double x) {
			// Welford-Knuth online variance algorithm
			n++;
			double delta = x - _mean;
			_mean += delta / n;
			double delta2 = x - mean;
			_mean2 += delta * delta2;

			if (n > 2)
				_var = _mean2 / (n - 1);
		}
	}

	public class ARModel {

		readonly Queue<double> previousValues = new Queue<double>();

		readonly double c;
		readonly double[] parameters;
		
		public ARModel(double c, double[] parameters) {
			this.c = c;
			this.parameters = parameters;
		}

		public void Update(double x) {
			// default to x
			double x_hat = x;
			
			// we have sufficient data to allow x_hat to be defined
			if (previousValues.Count == parameters.Length + 1) {
				previousValues.Dequeue();
				x_hat = 0;
				for (int i = parameters.Length-1; i >= 0; i--) {
					x_hat += parameters[i] * previousValues[i];
				}
				x_hat += c;
			}

			previousValues.Enqueue(x);

			return x_hat;
		}
	}
}