using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SharpBCI;
using UnityEngine;

namespace SharpBCI
{
	public interface IPreprocessor
	{
		double[] Preprocess (double[] data);

		string ToString ();
	}

	public class Preprocessors
	{

		public class NonePreprocessor : IPreprocessor
		{
			public double[] Preprocess (double[] data)
			{
				return data;
			}

			public override string ToString ()
			{
				return "None";
			}
		}

		public class NormalizationPreprocessor : IPreprocessor
		{
			private readonly double _minBoundary;
			private readonly double _maxBoundary;

			public NormalizationPreprocessor () : this (0, 1)
			{
			}

			public NormalizationPreprocessor (double minBoundary, double maxBoundary)
			{
				_minBoundary = minBoundary;
				_maxBoundary = maxBoundary;
			}

			public double[] Preprocess (double[] data)
			{

				var min = data.Min ();
				var max = data.Max ();
				var constFactor = (_maxBoundary - _minBoundary) / (max - min);

				return data.Select (x => (x - min) * constFactor + _minBoundary).ToArray ();
			}

			public override string ToString ()
			{
				return "Normalization";
			}
		}
	}

	public class StandardizationPreprocessor : IPreprocessor
	{
		public double[] Preprocess (double[] data)
		{
			var mean = data.Average ();
			var stdDev = Mathf.Sqrt ((float) data.Select (x => x - mean).Sum (x => x * x) / (data.Length - 1));

			return data.Select (x => (x - mean) / stdDev).ToArray ();
		}

		public override string ToString ()
		{
			return "Standardization";
		}
	}

	public class CentralizationPreprocessor : IPreprocessor
	{
		public double[] Preprocess (double[] data)
		{
			var avg = data.Average ();
			return data.Select (x => x - avg).ToArray ();
		}

		public override string ToString ()
		{
			return "Centralization";
		}
	}
}

