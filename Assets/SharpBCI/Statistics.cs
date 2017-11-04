using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpBCI {

	public static class StatsUtils {
		
		public static double SampleMean(double[] x) {
			return x.Average();
		}

		public static double SampleVar(double[] x) {
			return SampleVar(x, SampleMean(x));
		}
		
		public static double SampleVar(double[] x, double mu) {
			return x.Select((xi) => (xi - mu) * (xi - mu)).Sum() / (x.Length - 1);
		}

		public static double ACorr(uint k, double[] x) {
			var x_bar = SampleMean(x);
			var s_sq = SampleVar(x, x_bar);
			return ACorr(k, x, x_bar, s_sq);
		}

		public static double ACorr(uint k, double[] x, double mu, double sigmaSq) {
			var n = x.Length;
			var sum = 0.0;
			for (int t = 0; t < n - k; t++) {
				sum += (x[t] - mu) * (x[t + k] - mu);
			}
			return sum / ((n - k) * sigmaSq);
		}

		public static double[] FitAR(uint p, double[] x) {
			var x_bar = SampleMean(x);
			var s_sq = SampleVar(x, x_bar);

			// Yule-Walker fitting

			// a column vector of auto-correlations from 1 to p
			double[][] r = MatrixUtils.Create((int) p, 1);
			for (uint i = 1; i <= p; i++) {
				r[i-1][0] = ACorr(i, x, x_bar, s_sq);
			}

			// construct R, a system of equations to solve the PHI vector given r
			// p = 3 should look like
			// [ r0, r1, r2 ] 
			// [ r1, r0, r1 ]
			// [ r2, r1, r0 ]
			// note r0 = 1
			var R = MatrixUtils.Create((int)p, (int)p);
			for (int i = 0; i < p; i++) {
				int k = i;
				int step = -1;
				for (int j = 0; j < p; j++) {
					// accor(0) = 1
					if (k == 0) {
						R[i][j] = 1;
						step = 1;
					} 
					// acorr(x) = r[x-1]
					else {
						R[i][j] = r[k - 1][0];
					}
					k += step;
				}
			}

			// invert R to solve for phi
			R = MatrixUtils.Inverse(R);

			// R = phi * r => phi = R^-1 * r
			// R is a p x p matrix and r is a p x 1 matrix so result is p x 1 matrix (a column vector)
			var phi = MatrixUtils.Product(R, r);
			// we want a normal array (a sort of row vector) for portability so transpose the resulting column vector
			return MatrixUtils.Transpose(phi)[0];
		}
	}


	public class OnlineVariance {
		public double mean { get { return _mean; } }
		public double var { get { return _var; } }
		public bool isValid { get { return n > 2; } }

		uint n = 0;
		double _mean = 0.0;
		double _mean2 = 0.0;

		double _var = double.NaN;

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

		readonly IndexableQueue<double> previousValues = new IndexableQueue<double>();

		readonly double c;
		readonly double[] parameters;
		
		public ARModel(double c, double[] parameters) {
			this.c = c;
			this.parameters = parameters;
		}

		public double Predict(double x) {
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