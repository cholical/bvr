using System;

using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

namespace SharpBCI {

	/**
	 * An object which configures a SharpBCI object.
	 * Generally built with SharpBCIBuilder
	 * @see SharpBCIBuilder
	 */
	public class SharpBCIConfig {
		public EEGDeviceAdapter adapter;
		public int channels;
	}

	/**
	 * A build class for SharpBCIConfig
	 * @see SharpBCIConfig
	 * @see SharpBCI
	 */
	public class SharpBCIBuilder {
		EEGDeviceAdapter adapter;
		int channels;

		public SharpBCIBuilder EEGDeviceAdapter(EEGDeviceAdapter adapter) {
			this.adapter = adapter;
			return this;
		}

		public SharpBCIBuilder Channels(int channels) {
			this.channels = channels;
			return this;
		}

		public SharpBCIConfig Build() {
			SharpBCIConfig cfg = new SharpBCIConfig();
			cfg.adapter = adapter;
			cfg.channels = channels;
			return cfg;
		}
	}

	/**
	 * A generic event which indicates previously trained event occured
	 */
	public class TrainedEvent {
		public int id;
	}

	/**
	 *  This is the "main" class which you should create.
	 */
	public class SharpBCI {
		public const int WINDOW_SIZE = 1024;

		// out-facing delegates
		/**
		 * A delegate which receives raw events based on what was registered
		 * @see EEGEvent
		 */
		public delegate void SharpBCIRawHandler(EEGEvent evt);

		/**
		 * A delegate which recieved events based on a unique id returned by SharpBCI.StartTrain()
		 * @see SharpBCI.StartTrain()
		 * @see TrainedEvent
		 */
		public delegate void SharpBCITrainedHandler(TrainedEvent evt);

		// end out-facing delegates

		readonly EEGDeviceAdapter adapter;
		readonly int channels;

		readonly List<Pipeable> stages;

		float[] _connectionStatus;

		int nextId = 0;

		/**
		 * Is the device connected to a human
		 * Based on the Muse EEG status updates: 
		 * @returns 4 = no connection, 2 = ok connection, 1 = good connection, 3 = unused, complain to Muse about that
		 */
		public float[] connectionStatus { get { return _connectionStatus; } }

		/**
         * @param config a valid config object, generally built with SharpBCIBuilder
         * @see SharpBCIConfig
         * @see SharpBCIBuilder
		 */
		public SharpBCI(SharpBCIConfig config) {
			// begin check args
			if (config.adapter == null)
				throw new ArgumentException("config.adapter must not be null");

			if (channels <= 0)
				throw new ArgumentException("config.channels must be > 0");
			// end check args

			// begin state config
			adapter = config.adapter;
			channels = config.channels;
			_connectionStatus = new float[channels];
			for (int i = 0; i < channels; i++) {
				_connectionStatus[i] = 4;
			}
			// end state config

			// kinda a kludge: link up connection status output, flush is called in EEGDeviceProducer
			adapter.AddHandler(EEGDataType.CONTACT_QUALITY, UpdateConnectionStatus);

			// begin internal pipeline construction
			var producer = new EEGDeviceProducer(adapter);
			stages.Add(producer);

			var fft = new FFTPipeable(WINDOW_SIZE, channels);
			fft.Connect(producer);
			stages.Add(fft);

			// frequency src: en.wikipedia.org/wiki/Electroencephalography
			// in order of increasing frequency
			var deltaAbs = new FFTBandPipeable(0, 4, EEGDataType.DELTA_ABSOLUTE);
			deltaAbs.Connect(fft);
			stages.Add(deltaAbs);

			var thetaAbs = new FFTBandPipeable(4, 8, EEGDataType.THETA_ABSOLUTE);
			thetaAbs.Connect(fft);
			stages.Add(thetaAbs);

			var alphaAbs = new FFTBandPipeable(8, 16, EEGDataType.ALPHA_ABSOLUTE);
			alphaAbs.Connect(fft);
			stages.Add(alphaAbs);

			var betaAbs = new FFTBandPipeable(16, 32, EEGDataType.BETA_ABSOLUTE);
			betaAbs.Connect(fft);
			stages.Add(betaAbs);

			var gammaAbs = new FFTBandPipeable(32, 500, EEGDataType.GAMMA_ABSOLUTE);
			gammaAbs.Connect(fft);
			stages.Add(gammaAbs);

			// TODO other stages

			// end internal pipeline construction

			// begin start associated threads & EEGDeviceAdapter
			TaskFactory f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
			CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource();

			foreach (var stage in stages) {
				stage.Start(f, cts);
			}
			// end start associated threads & EEGDeviceAdapter
		}

		/**
		 * Start training SharpBCI on the EEG data from now on
		 * Should be paired w/ a StopTraining(id) call
		 * @returns The id which identifies the current training session
		 */
		public int StartTraining() {
			return nextId++;
		}

		/**
		 * Stop training SharpBCI on the current trainingID
		 */
		public void StopTraining(int id) {
			if (id < 0) throw new ArgumentException("Training id cannot be < 0");
		}

		public void AddTrainedHandler(int id, SharpBCITrainedHandler handler) { 
			if (id < 0) throw new ArgumentException("Training id cannot be < 0");

		}

		public void RemoveTrainedHandler(int id, SharpBCITrainedHandler handler) { 
			if (id< 0) throw new ArgumentException("Training id cannot be < 0");
		
		}

		public void AddRawHandler(EEGDataType type, SharpBCIRawHandler handler) { 
		
		}

		public void RemoveRawHandler(EEGDataType type, SharpBCIRawHandler handler) { 
		
		}

		/**
		 * Called when all the SharpBCI threads should shutdown.  
		 * You may or may not continue to receive events after calling this.
		 * You should unregister events before calling this: adjust your code accordingly.
		 */
		public void Close() {
			foreach (var stage in stages) {
				stage.Stop();
			}
		}

		void UpdateConnectionStatus(EEGEvent evt) {
			_connectionStatus = evt.data;
		}
	}

	public class EEGDeviceProducer : Pipeable {
		readonly EEGDeviceAdapter adapter;
		public EEGDeviceProducer(EEGDeviceAdapter adapter) {
			this.adapter = adapter;
		}

		public override void Start(TaskFactory taskFactory, CancellationTokenSource cts) {
			adapter.AddHandler(EEGDataType.EEG, Add);
			adapter.Start();
			base.Start(taskFactory, cts);
		}

		public override void Stop() {
			adapter.Stop();
			base.Stop();
		}

		protected override bool Process(object item) {
			adapter.FlushEvents();
			return true;
		}
	}
}
