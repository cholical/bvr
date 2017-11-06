using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpBCI {

	/**
	 * A class for statistical utility functions.
	 * Most are fairly naive in their calculation so not very performant.
	 */
	public static class StatsUtils {

		public static double SampleMean(double[] x) {
			if (x == null || x.Length == 0)
				throw new ArgumentOutOfRangeException();
			
			return x.Average();
		}

		public static double SampleVar(double[] x) {
			return SampleVar(x, SampleMean(x));
		}

		public static double SampleVar(double[] x, double mu) {
			if (x == null || x.Length < 2)
				throw new ArgumentOutOfRangeException();
			
			return x.Select((xi) => (xi - mu) * (xi - mu)).Sum() / (x.Length - 1);
		}

		/**
		 * Calculate ACF(k|x).  
		 * Estimates the population mean is sample mean 
		 * and population variance is unbiased sample variance
		 */
		public static double ACorr(uint k, double[] x) {
			var x_bar = SampleMean(x);
			var s_sq = SampleVar(x, x_bar);
			return ACorr(k, x, x_bar, s_sq);
		}

		/**
		 * Calculate ACF(k|x, mu, sigmaSq)
		 * Assumes the mean and variance of the population of X is known
		 */
		public static double ACorr(uint k, double[] x, double mu, double sigmaSq) {
			if (k <= 0)
				throw new ArgumentOutOfRangeException();
			
			var n = x.Length;
			var sum = 0.0;
			for (int t = 0; t < n - k; t++) {
				sum += (x[t] - mu) * (x[t + k] - mu);
			}
			return sum / ((n - k) * sigmaSq);
		}

		/**
		 * Find a value p such that PACF(i) for all i > p+1 is w/in 5% CI interval of zero
		 * Approximate performance = O(N^3) * maxOrder
		 */
		public static uint EstimateAROrder(double[] x, uint maxOrder) {
			//var p = x.Length / 4;
			double[] pacf = new double[maxOrder];
			for (uint i = 1; i <= maxOrder; i++) {
				pacf[i - 1] = PartialACorr(i, x);
			}

			var pacf_bar = SampleMean(pacf);
			var pacf_s = Math.Sqrt(SampleVar(pacf, pacf_bar));

			var minCutoff = pacf_bar - 2 * pacf_s;
			var maxCutoff = pacf_bar + 2 * pacf_s;

			Logger.Log("PACF 95% CI [{0}, {1}]", minCutoff, maxCutoff);

			for (uint i = 1; i < maxOrder; i++) {
				// find such an i in [1, p] that pacf(i+1) is zero w/in 5% CI
				if (pacf.Skip((int)i).All((pacf_i) => pacf_i >= minCutoff && pacf_i <= maxCutoff))
					return i;
			}
			// error case, throw exception??
			return maxOrder;
		}

		/**
		 * Calculate PACF(k|x)
		 * Uses a Yule-Walker Estimation of AR model, so is atleast O(N^3)
		 */
		public static double PartialACorr(uint k, double[] x) {
			return FitAR(k, x)[k - 1];
		}

		/**
		 * Try to fit an AR(p) model to x using Yule-Walker Estimation
		 * Since AR(0) = noise, does not support p == 0
		 */
		public static double[] FitAR(uint p, double[] x) {
			if (p <= 0)
				throw new ArgumentOutOfRangeException();
			
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

	/**
	 * Simultaneously calculates sample mean and variance in O(1)
	 * using Welford-Knuth online algorithm.
	 */
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

	/**
	 * A simple AR model class which can predict the next value of X given 
	 * AR parameters (phi), a constant factor (i.e., E[noise(X)]), and previous values of X
	 * O(p) performance
	 */
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