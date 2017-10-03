
namespace SharpBCI {
	public interface IFilter<T> {
		T Filter(T value);
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
}
