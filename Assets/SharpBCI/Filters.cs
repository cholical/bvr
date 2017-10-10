using System;

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

	public class PassThroughFilter : IFilter<double> {
		public double Filter(double val) { return val; }
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

	public abstract class ConvolutionalFilter : IFilter<double> { 
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

	public class NotchFilter : ConvolutionalFilter {

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
}
