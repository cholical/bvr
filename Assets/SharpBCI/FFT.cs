using System;
using System.Collections.Generic;

namespace SharpBCI {

	/**
	 * A Pipeable which performs an FFT on each channel
	 * It outputs an FFTEvent every windowSize samples
	 * @see FFTEvent
	 */
	public class FFTPipeable : Pipeable {

		// ~10 Hz @ 220Hz sampling
		public const int FFT_RATE = 22;

		readonly int windowSize;
		readonly int channels;

		// use a jagged array to make full-row access faster
		//readonly double[][] samples;

		readonly Queue<double>[] samples;

		readonly Lomont.LomontFFT FFT = new Lomont.LomontFFT();

		DateTime windowStart;
		DateTime windowEnd;

		int nSamples = 0;
		int lastFFT = 0;
		int totalSamples = 0;

		double sampleRate;

		IFilter<double>[] filters;


		/**
		 * Create a new FFTPipeable which performs an FFT over windowSize.  Expects an input pipeable of EEGEvent's
		 * @param windowSize The size of the FFT window, determines granularity (google FFT)
		 * @param channels How many channels to operate on
		 * @see EEGEvent
		 */
		public FFTPipeable(int windowSize, int channels, double sampleRate) {
			// 2 * windowSize to account for the imaginary parts of the FFT
			this.windowSize = 2 * windowSize;
			this.channels = channels;
			this.sampleRate = sampleRate;
				
			samples = new Queue<double>[channels];
			filters = new IFilter<double>[channels];
			for (int i = 0; i < channels; i++) {
				samples[i] = new Queue<double>();
				filters[i] = new PassThroughFilter();
			}
		}

		protected override bool Process(object item) {
			EEGEvent evt = (EEGEvent) item;
			if (evt.type != EEGDataType.EEG)
				throw new Exception("FFTFilter recieved invalid EEGEvent: " + evt);

			if (evt.data.Length != channels)
				throw new Exception("Malformed EEGEvent: " + evt);

			//Logger.Log ("Recording samples, nSamples=" + nSamples + ", evt=" + evt);
			totalSamples += 2;
			if (totalSamples % windowSize == 0) {
				windowStart = windowEnd;
				windowEnd = evt.timestamp;
			}

			// normal case: just append data to sample buffer
			for (int i = 0; i < channels; i++) {
				samples[i].Enqueue(filters[i].Filter(evt.data[i]));
				// this is the imaginary part of the signal, but we're FFT-ing a real number so 0 for us
				// TODO is this ALWAYS true?
				samples[i].Enqueue(0);
			}
			nSamples += 2;

			if (nSamples >= windowSize + 2) {
				foreach (var channelSamples in samples) {
					channelSamples.Dequeue();
					channelSamples.Dequeue();
				}
				nSamples -= 2;
			}

			lastFFT++;
			// sample buffer is full, do FFT then reset for next round
			if (nSamples >= windowSize && lastFFT % FFT_RATE == 0) {
				// Do an FFT on each channel
				List<double[]> fftOutput = new List<double[]>();
				foreach (var channelSamples in samples) {
					// Logger.Log("FFT sample size:" + channelSamples.Count);
					var samplesCopy = channelSamples.ToArray();
					FFT.FFT(samplesCopy, true);
					fftOutput.Add(samplesCopy);
				}

				// find sampleRate given windowStart and windowEnd there are only windowSize / 2 actual samples
				//double sampleRate = (windowSize / 2) / windowEnd.Subtract(windowStart).TotalSeconds;
				//Logger.Log(string.Format("Current sampleRate: {0:#} Hz", sampleRate));

				// find abs powers for each band
				Dictionary<EEGDataType, List<double>> absolutePowers = new Dictionary<EEGDataType, List<double>>();
				foreach (var bins in fftOutput) {
					double deltaAbs = AbsBandPower(bins, 1, 4, sampleRate);
					double thetaAbs = AbsBandPower(bins, 4, 8, sampleRate);
					double alphaAbs = AbsBandPower(bins, 7.5, 13, sampleRate);
					double betaAbs = AbsBandPower(bins, 13, 30, sampleRate);
					double gammaAbs = AbsBandPower(bins, 30, 44, sampleRate);

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
				Add(new EEGEvent(evt.timestamp, EEGDataType.BETA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.BETA_ABSOLUTE)));
				Add(new EEGEvent(evt.timestamp, EEGDataType.GAMMA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.GAMMA_ABSOLUTE)));
				Add(new EEGEvent(evt.timestamp, EEGDataType.DELTA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.DELTA_ABSOLUTE)));
				Add(new EEGEvent(evt.timestamp, EEGDataType.THETA_RELATIVE, RelBandPower(absolutePowers, EEGDataType.THETA_ABSOLUTE)));
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
			int N = bins.Length / 2;
			// freq = (i/2) * sampleRate / N => i = 2 * freq / (sampleRate / N)
			int minBin = Math.Max(1, 2 * (int)Math.Floor(minFreq / (sampleRate / N)));
			int maxBin = Math.Min(N / 2, 2 * (int)Math.Ceiling(maxFreq / (sampleRate / N)));
			double powerSum = 0;
			for (int i = minBin; i < maxBin; i += 2) {
				powerSum += Math.Abs(bins[i]);
			}
			return powerSum;
		}
	}
}
