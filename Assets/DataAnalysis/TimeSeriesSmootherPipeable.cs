using System.Collections;

namespace SharpBCI
{
	public class TimeSeriesSmootherPipeable : Pipeable
	{
		private TimeSeriesSmoother smoother;

		public TimeSeriesSmootherPipeable(TimeSeriesSmoother s) {
			smoother = s;
		}

		public TimeSeriesSmootherPipeable() {
			smoother = new SingleExponentialSmoother(.5);
		}
			
		protected override bool Process (object item)
		{
			if (!(item is double)) {
				return false;
			}
			this.Add (smoother.next((double) item));
			return true;
		}

	}
}
