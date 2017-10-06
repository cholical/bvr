using System;
using System.Linq;
using System.Collections.Generic;

namespace SharpBCI {

	/**
	 * A Pipeable which performs an FFT on each channel
	 * It outputs an FFTEvent every windowSize samples
	 * @see FFTEvent
	 */
	public class FFTPipeable : Pipeable {
		
		public const double HAMMING_ALPHA = 25 / 46;
		public const double HAMMING_BETA = 21 / 46;

		public static double HammingWindow(int i, int N) {
			return HAMMING_ALPHA - HAMMING_BETA * Math.Cos((2 * Math.PI * i) / (N - 1));
		}

		public static double BoxcarWindow(int i, int N) {
			return 1;
		}
		
		readonly int windowSize;
		readonly int channels;
		readonly Queue<double>[] samples;
		readonly Lomont.LomontFFT FFT = new Lomont.LomontFFT();
		readonly int fftRate;
		readonly double sampleRate;
		readonly IFilter<double>[] filters;
		readonly double[] windowConstants;

		int nSamples = 0;
		int lastFFT = 0;

		/**
		 * Create a new FFTPipeable which performs an FFT over windowSize.  Expects an input pipeable of EEGEvent's
		 * @param windowSize The size of the FFT window, determines granularity (google FFT)
		 * @param channels How many channels to operate on
		 * @see EEGEvent
		 */
		public FFTPipeable(int windowSize, int channels, double sampleRate) {
			// 2 * windowSize to account for the imaginary parts of the FFT
			windowSize = 2 * windowSize;

			this.windowSize = windowSize;
			this.channels = channels;
			this.sampleRate = sampleRate;

			// target 10Hz
			fftRate = (int)Math.Round(sampleRate / 10);

			samples = new Queue<double>[channels];
			filters = new IFilter<double>[channels];

			for (int i = 0; i < channels; i++) {
				samples[i] = new Queue<double>();
				filters[i] = new PassThroughFilter();
			}

			windowConstants = new double[windowSize];
			for (int i = 0; i < windowSize; i++) {
				windowConstants[i] = BoxcarWindow(i, windowSize);
			}
		}

		protected override bool Process(object item) {
			EEGEvent evt = (EEGEvent) item;
			if (evt.type != EEGDataType.EEG)
				throw new Exception("FFTPipeable recieved invalid EEGEvent: " + evt);

			if (evt.data.Length != channels)
				throw new Exception("FFTPipeable recieved malformed EEGEvent: " + evt);

			// normal case: just append data to sample buffer
			for (int i = 0; i < channels; i++) {
				samples[i].Enqueue(evt.data[i]);
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
			if (nSamples >= windowSize && lastFFT % fftRate == 0) {
				DoFFT(evt);
			}

			return true;
		}

		void DoFFT(EEGEvent evt) { 
			// Do an FFT on each channel
			List<double[]> fftOutput = new List<double[]>();
			foreach (var channelSamples in samples) {
				// Logger.Log("FFT sample size:" + channelSamples.Count);
				var samplesCopy = channelSamples.ToArray();
				// apply hamming windowing function to samplesCopy
				//ApplyWindow(samplesCopy);

				FFT.FFT(samplesCopy, true);

				// now we want to slice off the complex conjugates and get magnitude of each complex number
				int nMags = windowSize / 2;
				var magnitudes = new double[nMags];
				int i = 0;
				int j = 0;
				while (i < nMags) {
					var abs = Math.Sqrt(samplesCopy[i] * samplesCopy[i] + samplesCopy[i + 1] * samplesCopy[i + 1]);
					magnitudes[j++] = abs;
					i += 2;
				}
				fftOutput.Add(magnitudes);
			}

			for (int i = 0; i<fftOutput.Count; i++) {
				Add(new EEGEvent(evt.timestamp, EEGDataType.FFT_RAW, fftOutput[i], i));
			}

			// find sampleRate given windowStart and windowEnd there are only windowSize / 2 actual samples
			//double sampleRate = (windowSize / 2) / windowEnd.Subtract(windowStart).TotalSeconds;
			//Logger.Log(string.Format("Current sampleRate: {0:#} Hz", sampleRate));

			// find abs powers for each band
			Dictionary<EEGDataType, List<double>> absolutePowers = new Dictionary<EEGDataType, List<double>>();
			foreach (var bins in fftOutput) {
				double deltaAbs = AbsBandPower(bins, 1, 4);
				double thetaAbs = AbsBandPower(bins, 4, 8);
				double alphaAbs = AbsBandPower(bins, 7.5, 13);
				double betaAbs = AbsBandPower(bins, 13, 30);
				double gammaAbs = AbsBandPower(bins, 30, 44);

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

		void ApplyWindow(double[] arr) {
			int N = arr.Length;
			for (int i = 0; i < N; i += 2) {
				arr[i] = arr[i] * windowConstants[i];
			}
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

		double AbsBandPower(double[] bins, double minFreq, double maxFreq) {
			int minBin = (int)Math.Floor(minFreq / sampleRate);
			int maxBin = (int)Math.Ceiling(maxFreq / sampleRate);
			double powerSum = 0;
			for (int i = minBin; i < maxBin; i++) {
				powerSum += bins[i];
			}
			return powerSum;
		}
	}
}
