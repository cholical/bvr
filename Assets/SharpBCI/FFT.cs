using System;
using System.Collections.Generic;

namespace SharpBCI {

	/**
	 * A simple event class which represents the output of an FFT over an arbitrary amount of channels
	 * Probably shouldn't mod the fields unless you know what you're doing
	 */
	public class FFTEvent {
		public DateTime timestamp;
		public double[][] bins;
		public double sampleRate;

		public FFTEvent(DateTime timestamp, double sampleRate, double[][] bins) {
			this.timestamp = timestamp;
			this.sampleRate = sampleRate;
			this.bins = bins;
		}
	}

	/**
	 * A Pipeable which performs an FFT on each channel
	 * It outputs an FFTEvent every windowSize samples
	 * @see FFTEvent
	 */
	public class FFTPipeable : Pipeable {

		readonly int windowSize;
		readonly int channels;
		// use a jagged array to make full-row access faster
		readonly double[][] samples;

		readonly Lomont.LomontFFT FFT = new Lomont.LomontFFT();

		//readonly Converter<float, double> doubleConverter = new Converter<float, double>(delegate(float input) {
		//	return (double)input;
		//});

		int nSamples = 0;
		DateTime windowStart;
		DateTime windowEnd;

		/**
		 * Create a new FFTPipeable which performs an FFT over windowSize.  Expects an input pipeable of EEGEvent's
		 * @param windowSize The size of the FFT window, determines granularity (google FFT)
		 * @param channels How many channels to operate on
		 * @see EEGEvent
		 */
		public FFTPipeable(int windowSize, int channels) {
			// 2 * windowSize to account for the imaginary parts of the FFT
			this.windowSize = 2 * windowSize;
			this.channels = channels;

			samples = new double[channels][];
			for (int i = 0; i < channels; i++) {
				samples[i] = new double[windowSize];
			}
		}

		protected override bool Process(object item) {
			EEGEvent evt = (EEGEvent) item;
			if (evt.type != EEGDataType.EEG)
				throw new Exception("FFTFilter recieved invalid EEGEvent: " + evt);

			// for calculating sampleRate
			if (nSamples == 0) {
				windowStart = evt.timestamp;
			}

			// normal case: just append data to sample buffer
			for (int i = 0; i < channels; i++) {
				samples[i][nSamples++] = evt.data[i];
				// this is the imaginary part of the signal, but we're FFT-ing a real number so 0 for us
				// TODO is this ALWAYS true?
				samples[i][nSamples++] = 0;
			}

			// sample buffer is full, do FFT then reset for next round
			if (nSamples == windowSize) {
				windowEnd = evt.timestamp;

				// Do an FFT on each channel
				List<double[]> fftOutput = new List<double[]>();
				foreach (var channelSamples in samples) {
					// TODO is a clone necessary?
					// var samplesCopy = (double[]) channelSamples.Clone();
					FFT.FFT(channelSamples, true);
					fftOutput.Add(channelSamples);
				}

				// find sampleRate given windowStart and windowEnd there are only windowSize / 2 actual samples
				double sampleRate = (windowSize / 2) / windowEnd.Subtract(windowStart).TotalSeconds;

				// find abs powers for each band
				Dictionary<EEGDataType, List<double>> absolutePowers = new Dictionary<EEGDataType, List<double>>();
				foreach (var bins in fftOutput) {
					double deltaAbs = AbsBandPower(bins, 0, 4, sampleRate);
					double thetaAbs = AbsBandPower(bins, 4, 8, sampleRate);
					double alphaAbs = AbsBandPower(bins, 8, 16, sampleRate);
					double betaAbs = AbsBandPower(bins, 16, 32, sampleRate);
					double gammaAbs = AbsBandPower(bins, 32, 0, sampleRate);

					GetBandList(absolutePowers, EEGDataType.ALPHA_ABSOLUTE).Add(alphaAbs);
					GetBandList(absolutePowers, EEGDataType.BETA_ABSOLUTE).Add(betaAbs);
					GetBandList(absolutePowers, EEGDataType.GAMMA_ABSOLUTE).Add(gammaAbs);
					GetBandList(absolutePowers, EEGDataType.DELTA_ABSOLUTE).Add(deltaAbs);
					GetBandList(absolutePowers, EEGDataType.THETA_ABSOLUTE).Add(thetaAbs);

				}

				// we can emit abs powers immediately
				Add(new EEGEvent(evt.timestamp, EEGDataType.ALPHA_ABSOLUTE, absolutePowers[EEGDataType.ALPHA_ABSOLUTE].ToArray()));
				Add(new EEGEvent(evt.timestamp, EEGDataType.BETA_ABSOLUTE, absolutePowers[EEGDataType.BETA_ABSOLUTE].ToArray()));
				Add(new EEGEvent(evt.timestamp, EEGDataType.GAMMA_ABSOLUTE, absolutePowers[EEGDataType.GAMMA_ABSOLUTE].ToArray()));
				Add(new EEGEvent(evt.timestamp, EEGDataType.DELTA_ABSOLUTE, absolutePowers[EEGDataType.DELTA_ABSOLUTE].ToArray()));
				Add(new EEGEvent(evt.timestamp, EEGDataType.THETA_ABSOLUTE, absolutePowers[EEGDataType.THETA_ABSOLUTE].ToArray()));

				// now calc and emit relative powers
				Add(new EEGEvent(evt.timestamp, EEGDataType.ALPHA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.ALPHA_ABSOLUTE)));
				Add(new EEGEvent(evt.timestamp, EEGDataType.BETA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.BETA_RELATIVE)));
				Add(new EEGEvent(evt.timestamp, EEGDataType.GAMMA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.GAMMA_RELATIVE)));
				Add(new EEGEvent(evt.timestamp, EEGDataType.DELTA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.DELTA_RELATIVE)));
				Add(new EEGEvent(evt.timestamp, EEGDataType.THETA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.THETA_RELATIVE)));

				// now write over all the data by resetting nSamples, this way we don't have to waste time clearing
				// NOTE: after FFT stage samples now contains freq-domain data not time-domain data,
				// but it will be completely overwritten before next FFT
				nSamples = 0;
			}

			return true;
		}

		List<double> GetBandList(Dictionary<EEGDataType, List<double>> dict, EEGDataType type) {
			if (dict.ContainsKey(type)) {
				return dict[type];
			} else {
				List<double> val = new List<double>();
				dict[type] = val;
				return val;
			}
		}

		double[] RelBandPower(Dictionary<EEGDataType, List<double>> powerDict, EEGDataType band) {
			double[] absPowers = new double[channels];
			foreach (var channelPowers in powerDict.Values) {
				for (int i = 0; i < channels; i++) {
					absPowers[i] += channelPowers[i];
				}
			}

			double[] relPowers = new double[channels];
			List<double> bandPowers = powerDict[band];
			for (int i = 0; i < channels; i++) {
				relPowers[i] = bandPowers[i] / absPowers[i];
			}
			return relPowers;
		}

		double AbsBandPower(double[] bins, double minFreq, double maxFreq, double sampleRate) {
			int N = bins.Length;
			int minBin = Math.Max(1, 2 * (int)Math.Floor(minFreq / (sampleRate / N)));
			int maxBin = Math.Min(N/2, 2 * (int)Math.Ceiling(maxFreq / (sampleRate / N)));
			double powerSum = 0;
			for (int i = minBin; i < maxBin; i += 2) {
				powerSum += bins[i];;
			}
			return powerSum;
		}
	}
}
