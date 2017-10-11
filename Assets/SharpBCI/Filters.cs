using System;
using System.Collections.Generic;

namespace SharpBCI {
	public interface IFilter<T> {
		T Filter(T value);
	}

	public class MultiFilter<T> : IFilter<T> {

		readonly IFilter<T>[] filters;

		public MultiFilter(IFilter<T>[] filters) {
			this.filters = filters;
		}

		public T Filter(T val) {
			foreach (var f in filters) {
				val = f.Filter(val);
			}
			return val;
		}
	}

	public class MovingAverageFilter : IFilter<double> {
		readonly Queue<double> samples = new Queue<double>();
		readonly int windowSize;
		double accumulator;

		public MovingAverageFilter(int windowSize) {
			this.windowSize = windowSize;
		}

		public double Filter(double val) {
			samples.Enqueue(val);
			accumulator += val;
			if (samples.Count == windowSize + 1) {
				accumulator -= samples.Dequeue();
				return accumulator / windowSize;
			} else {
				return val;
			}
		}
	}

	public class ExponentialFilter : IFilter<double> {
		readonly double alpha;

		double lastVal;

		public ExponentialFilter(double alpha) {
			this.alpha = alpha;
		}

		public double Filter(double val) {
			var nextVal = alpha * val + (1 - alpha) * lastVal;
			lastVal = nextVal;
			return nextVal;
		}
	}

	public abstract class RecursiveFilter : IFilter<double> { 
		double a0, a1, a2, b1, b2;

		double x1, x2, y1, y2;

		bool initialized;

		public double Filter(double x0) {
			if (!initialized) { 
                CalcParams(out a0, out a1, out a2, out b1, out b2);
				initialized = true;
			}

			double y0 = a0 * x0 + a1 * x1 + a2 * x2 + b1 * y1 + b2 * y2;
			x2 = x1;
			x1 = x0;

			y2 = y1;
			y1 = y0;

			//Logger.Log("x0={0}, x1={1}, x2={2}, y0={3}, y1={4}, y2={5}", x0, x1, x2, y0, y1, y2);
			return y0;
		}

		protected abstract void CalcParams(out double a0, out double a1, out double a2, out double b1, out double b2);
	}

	public class NotchFilter : RecursiveFilter {

		readonly double center;
		readonly double bandwidth;

		public NotchFilter(double fMin, double fMax, double sampleRate) {
			if (fMax <= fMin)
				throw new ArgumentException("fMax must be > fMin");

			bandwidth = fMax - fMin;
			center = (bandwidth / 2.0) + fMin;

			//Logger.Log ("Created NotchFilter with bandwidth of {0} @ {1}", bandwidth, center);
			// express both of these as a ratio of the sampling rate
			bandwidth /= sampleRate;
			center /= sampleRate; 
		}

		protected override void CalcParams(out double a0, out double a1, out double a2, out double b1, out double b2) {
			// calculate recursion coeffs.
			var R = (1 - 3 * bandwidth);
			var K = (1 - 2 * R * Math.Cos(2 * Math.PI * center) + R * R) / (2 - 2 * Math.Cos(2 * Math.PI * center));
			a0 = K;
			a1 = -2 * K * Math.Cos(2 * Math.PI* center);
			a2 = K;
			b1 = 2 * R * Math.Cos(2 * Math.PI* center);
			b2 = -(R * R);

			//Logger.Log("NotchFilter params: a0={0}, a1={1}, a2={2}, b1={3}, b2={4}", a0, a1, a2, b1, b2);
		}
	}

	public class WindowedSincFilter : IFilter<double> {

		readonly double[] H;
		readonly Queue<double> lastValues = new Queue<double>();

		public WindowedSincFilter(double fCutoff, double bandwidth, double sampleRate) {
			fCutoff /= sampleRate;
			bandwidth /= sampleRate;

			int M = (int)Math.Round(4 / bandwidth);

			H = new double[M];
			for (int i = 0; i < M; i++) {
				if (i - M / 2 == 0) {
					H[i] = 2 * Math.PI * fCutoff;
				} else {
					H[i] = Math.Sin(2 * Math.PI * fCutoff * (i - M / 2)) / (i - M / 2);
					H[i] = H[i] * (0.54 - 0.46 * Math.Cos(2 * Math.PI * i / M));
				}
			}

			double sum = 0;
			for (int i = 0; i < M; i++) {
				sum += H[i];
			}

			for (int i = 0; i < M; i++) {
				H[i] /= sum;
			}
		}

		public double Filter(double val) {
			lastValues.Enqueue(val);
			if (lastValues.Count == H.Length + 1) {
				lastValues.Dequeue();

				// convolve X to Y
				var arr = lastValues.ToArray();
				double y = 0;
				for (int i = 0; i < H.Length; i++) {
					y += arr[H.Length - i - 1] * H[i];
				}
				return y;
			}
			return val;
		}
	}
}
