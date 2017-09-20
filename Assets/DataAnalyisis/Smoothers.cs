using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SharpBCI;

namespace SharpBCI
{
	public class Smoothers
	{

		public abstract class TimeSeriesSmoother
		{
			protected double current;

			public abstract double next (double x);

			public double getAverage ()
			{
				return current;
			}
		}

		public class SimpleMovingAverageSmoother : TimeSeriesSmoother
		{

			private int windowSize;

			private Queue<double> window;

			public SimpleMovingAverageSmoother (int win)
			{

				if (win < 1) {
					win = 1;
				}

				windowSize = win;
				window = new Queue<double> ();
			}

			public override double next (double x)
			{
				window.Enqueue (x);

				if (window.Count > windowSize) {
					window.Dequeue ();
				}

				current = window.Average ();

				return current;

			}
		}

		public class SingleExponentialSmoother : TimeSeriesSmoother
		{
			public double alpha;

			public SingleExponentialSmoother (double a)
			{
				alpha = a;
			}

			public override double next (double x)
			{
				if (current.Equals (null)) {
					current = x;
				} else {
					current = (alpha * x) + ((1 - alpha) * current);
				}
				return current;
			}
		}
	}
}

