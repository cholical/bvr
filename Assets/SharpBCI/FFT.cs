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
		readonly float[][] samples;

		readonly Lomont.LomontFFT FFT = new Lomont.LomontFFT();

		readonly Converter<float, double> doubleConverter = new Converter<float, double>(delegate(float input) {
			return (double)input;
		});

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

			samples = new float[channels][];
			for (int i = 0; i < channels; i++) {
				samples[i] = new float[windowSize];
			}
		}

		protected override bool Process(object item) {
			EEGEvent evt = (EEGEvent) item;
			if (evt.type != EEGDataType.EEG)
				throw new Exception("FFTFilter recieved invalid EEGEvent: " + evt);

			if (nSamples == 0) {
				windowStart = evt.timestamp;
			}

			for (int i = 0; i < channels; i++) {
				samples[i][nSamples++] = evt.data[i];
				// this is the imaginary part of the signal, but we're FFT-ing a real number so 0 for us
				// TODO is this ALWAYS true?
				samples[i][nSamples++] = 0;
			}

			if (nSamples == windowSize) {
				windowEnd = evt.timestamp;
				List<double[]> fftOutput = new List<double[]>();
				foreach (var channelSamples in samples) {
					double[] samplesCopy = Array.ConvertAll<float, double>(channelSamples, doubleConverter);
					FFT.FFT(samplesCopy, true);
					fftOutput.Add(samplesCopy);
				}
				double sampleRate = (windowSize / 2) / windowEnd.Subtract(windowStart).TotalSeconds;
				var fftEvt = new FFTEvent(evt.timestamp, sampleRate, fftOutput.ToArray());
				Add(fftEvt);
				nSamples = 0;
			}

			return true;
		}
	}

	public class FFTBandPipeable : Pipeable {

		readonly double minFreq;
		readonly double maxFreq;
		readonly EEGDataType type;

		public FFTBandPipeable(double minFreq, double maxFreq, EEGDataType type) {
			this.minFreq = minFreq;
			this.maxFreq = maxFreq;
		}

		protected override bool Process(object item) {
			FFTEvent evt = (FFTEvent) item;
			double sampleRate = evt.sampleRate;
			List<float> absPowers = new List<float>();
			foreach (var bins in evt.bins) {
				int N = bins.Length;
				int minBin = 2 * (int) Math.Floor(minFreq / (sampleRate / N));
				int maxBin = 2 * (int) Math.Ceiling(maxFreq / (sampleRate / N));
				double powerSum = 0;
				for (int i = minBin; i < maxBin && i < N / 2; i += 2) {
					powerSum += bins[i];
				}
				absPowers.Add((float) powerSum);
			}
			Add(new EEGEvent(evt.timestamp, type, absPowers.ToArray()));
			return true;
		}
	}
}
