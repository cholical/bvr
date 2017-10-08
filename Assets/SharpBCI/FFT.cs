using System;
using System.Linq;
using System.Collections.Generic;
using DSPLib;

namespace SharpBCI {

	/**
	 * A Pipeable which performs an FFT on each channel
	 * It outputs an FFTEvent every windowSize samples
	 * @see FFTEvent
	 */
	public class FFTPipeable : Pipeable {
		
		readonly uint windowSize;
		readonly uint channels;
		readonly uint fftRate;

		readonly Queue<double>[] samples;
		readonly FFT fft;

		readonly double sampleRate;
		readonly double[] windowConstants;
		readonly double scaleFactor;
		readonly double noiseFactor;

		readonly IFilter<double>[] filters;

		uint nSamples = 0;
		uint lastFFT = 0;

		public FFTPipeable(int windowSize, int channels, double sampleRate) : this(windowSize, channels, sampleRate, 10) { }

		/**
		 * Create a new FFTPipeable which performs an FFT over windowSize.  Expects an input pipeable of EEGEvent's
		 * @param windowSize The size of the FFT window, determines granularity (google FFT)
		 * @param channels How many channels to operate on
		 * @param sampleRate Sampling rate of data
		 * @param targetFFTRate Optional: Frequency (in Hz) to perform an FFT (exact frequency may vary)
		 * @see EEGEvent
		 */
		public FFTPipeable(int windowSize, int channels, double sampleRate, double targetFFTRate) {
			//this.windowSize = windowSize;

			this.windowSize = (uint) windowSize;
			this.channels = (uint)channels;
			this.sampleRate = sampleRate;

			fft = new FFT();
			fft.Initialize((uint) windowSize);
			//FFT.A = 0;
			//FFT.B = 1;

			// target 10Hz
			fftRate = (uint)Math.Round(sampleRate / targetFFTRate);

			samples = new Queue<double>[channels];
			filters = new IFilter<double>[channels];

			for (int i = 0; i < channels; i++) {
				samples[i] = new Queue<double>();
				filters[i] = new PassThroughFilter();
			}

			windowConstants = DSP.Window.Coefficients(DSP.Window.Type.Rectangular, this.windowSize);
			scaleFactor = DSP.Window.ScaleFactor.Signal(windowConstants);
			noiseFactor = DSP.Window.ScaleFactor.Noise(windowConstants, sampleRate);
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
				//samples[i].Enqueue(0);
			}
			nSamples++;

			if (nSamples > windowSize) {
				foreach (var channelSamples in samples) {
					channelSamples.Dequeue();
					//channelSamples.Dequeue();
				}
				nSamples--;
			}

			lastFFT++;
			//Logger.Log("nSamples={0}, lastFFT={1}", nSamples, lastFFT);
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
				var samplesCopy = channelSamples.ToArray();

				// apply windowing function to samplesCopy
				DSP.Math.Multiply(samplesCopy, windowConstants);

				var cSpectrum = fft.Execute(samplesCopy);
				double[] lmSpectrum = DSP.ConvertComplex.ToMagnitude(cSpectrum);
				lmSpectrum = DSP.Math.Multiply(lmSpectrum, scaleFactor);

				fftOutput.Add(lmSpectrum);
			}

			for (int i = 0; i<fftOutput.Count; i++) {
				Add(new EEGEvent(evt.timestamp, EEGDataType.FFT_RAW, fftOutput[i], i));
				//Add(new EEGEvent(evt.timestamp, EEGDataType.FFT_SMOOTHED, DSP.Math.Multiply(fftOutput[i], noiseFactor), i));
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
			for (int i = 0; i < N; i++) {
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
