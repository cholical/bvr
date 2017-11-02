using System;
using System.Collections.Generic;

namespace SharpBCI {
	
	public class SimpleFilterPipeable : Pipeable {
		
		readonly IFilter<double>[] signalFilters;

		public SimpleFilterPipeable(double sampleRate, int channels, double minFreq, double maxFreq, double transitionBandwidth) {
			
			signalFilters = new IFilter<double>[channels];
			for (int i = 0; i < channels; i++) {
				signalFilters[i] = new ConvolvingDoubleEndedFilter(minFreq, maxFreq, transitionBandwidth, sampleRate, true);
			}

		}

		protected override bool Process(object obj) {
			EEGEvent evt = (EEGEvent) obj;
			
			double[] buffer = new double[evt.data.Length];
			for (int i = evt.data.Length-1; i >= 0; i--) {
				buffer[i] = signalFilters[i].Filter(evt.data[i]);
			}

			// TODO extra artifact detection

			Add(new EEGEvent(evt.timestamp, evt.type, buffer, evt.extra));

			return true;
		}
	}
}