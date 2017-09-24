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

		public SharpBCIConfig(EEGDeviceAdapter adapter, int channels) {
			this.adapter = adapter;
			this.channels = channels;
		}
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

		public SharpBCI Build() {
			return new SharpBCI(new SharpBCIConfig(adapter, channels));
		}
	}

	/**
	 * A generic event which indicates previously trained event occured
	 */
	public class TrainedEvent {
		public int id;

		public TrainedEvent(int i) {
			id = i;
		}
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

		readonly List<Pipeable> stages = new List<Pipeable>();

		double[] _connectionStatus;

		int nextId = 0;

		readonly Dictionary<EEGDataType, List<SharpBCIRawHandler>> rawHandlers = new Dictionary<EEGDataType, List<SharpBCIRawHandler>>();
		readonly Dictionary<int, List<SharpBCITrainedHandler>> trainedHandlers = new Dictionary<int, List<SharpBCITrainedHandler>>();

		readonly TaskFactory taskFactory;
		readonly CancellationTokenSource cts;

		/**
		 * Is the device connected to a human
		 * Based on the Muse EEG status updates: 
		 * @returns 4 = no connection, 2 = ok connection, 1 = good connection, 3 = unused, complain to Muse about that
		 */
		public double[] connectionStatus { get { return _connectionStatus; } }

		//Pipeable to train on.
		private readonly Pipeable predictor;

		/**
         * @param config a valid config object, generally built with SharpBCIBuilder
         * @see SharpBCIConfig
         * @see SharpBCIBuilder
		 */
		public SharpBCI(SharpBCIConfig config) {
			// begin check args
			if (config.adapter == null)
				throw new ArgumentException("config.adapter must not be null");

			if (config.channels <= 0)
				throw new ArgumentException("config.channels must be > 0");
			// end check args

			// begin state config
			adapter = config.adapter;
			channels = config.channels;
			_connectionStatus = new double[channels];
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

			var rawEvtEmmiter = new RawEventEmitter(this);
			producer.Connect(rawEvtEmmiter);
			fft.Connect(rawEvtEmmiter);

			int bufferSize = 50;
			var predict = new KNearestNeighborPipeable(bufferSize);
			predictor = predict;
			rawEvtEmmiter.Connect(predict);


			var trainedEvtEmitter = new TrainedEventEmitter(this);
			// TODO other stages

			// end internal pipeline construction

			// begin start associated threads & EEGDeviceAdapter
			taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
			cts = new CancellationTokenSource();

			foreach (var stage in stages) {
				stage.Start(taskFactory, cts);
			}
			// end start associated threads & EEGDeviceAdapter
		}

		/**
		 * Start training SharpBCI on the EEG data from now on
		 * Should be paired w/ a StopTraining(id) call
		 * @returns The id which identifies the current training session
		 */
		public void StartTraining(int id) {
			predictor.StartTraining(id);
		}

		/**
		 * Stop training SharpBCI on the current trainingID
		 */
		public void StopTraining(int id) {
			if (id < 0 || id >= nextId) throw new ArgumentException("Training id invalid");
			
			predictor.StopTraining(id);
		}

		public void AddTrainedHandler(int id, SharpBCITrainedHandler handler) { 
			if (id < 0 || id >= nextId) throw new ArgumentException("Training id invalid");
			lock (trainedHandlers) {
				if (!trainedHandlers.ContainsKey(id))
					trainedHandlers.Add(id, new List<SharpBCITrainedHandler>());
				trainedHandlers[id].Add(handler);
			}
		}

		public void RemoveTrainedHandler(int id, SharpBCITrainedHandler handler) { 
			if (id < 0 || id >= nextId) throw new ArgumentException("Training id invalid");
			lock (trainedHandlers) {
				if (!trainedHandlers.ContainsKey(id))
					throw new ArgumentException("No handlers registered for id: " + id);
				if (!trainedHandlers[id].Remove(handler))
					throw new ArgumentException("Handler '" + handler + "' not registered for id: " + id);
			}
		}

		public void AddRawHandler(EEGDataType type, SharpBCIRawHandler handler) {
			lock (rawHandlers) {
				if (!rawHandlers.ContainsKey(type))
					rawHandlers.Add(type, new List<SharpBCIRawHandler>());
				rawHandlers[type].Add(handler);
			}
		}

		public void RemoveRawHandler(EEGDataType type, SharpBCIRawHandler handler) {
			lock (rawHandlers) {
				if (!rawHandlers.ContainsKey(type))
					throw new ArgumentException("No handlers registered for type: " + type);
				if (!rawHandlers[type].Remove(handler))
					throw new ArgumentException("Handler '" + handler + "' not registered for EEGDataType: " + type);
			}
		}

		/**
		 * Called when all the SharpBCI threads should shutdown.  
		 * You may or may not continue to receive events after calling this.
		 * You should unregister events before calling this: adjust your code accordingly.
		 */
		public void Close() {
			cts.Cancel();
			foreach (var stage in stages) {
				stage.Stop();
			}
		}

		void UpdateConnectionStatus(EEGEvent evt) {
			_connectionStatus = evt.data;
		}

		protected class TrainedEventEmitter : Pipeable { 
			readonly SharpBCI self;

			public TrainedEventEmitter(SharpBCI self) {
				this.self = self;
			}

			protected override bool Process(object item) {
				TrainedEvent evt = (TrainedEvent) item;
				lock (self.trainedHandlers) { 
					if (!self.trainedHandlers.ContainsKey(evt.id))
						return true;

					foreach (var handler in self.trainedHandlers[evt.id]) {
						handler(evt);
					}
				}
				return true;
			}
		}

		protected class RawEventEmitter : Pipeable {
			readonly SharpBCI self;

			public RawEventEmitter(SharpBCI self) {
				this.self = self;
			}

			protected override bool Process(object item) {
				EEGEvent evt = (EEGEvent) item;
				lock (self.rawHandlers) {
					if (!self.rawHandlers.ContainsKey(evt.type))
						return true;

					foreach (var handler in self.rawHandlers[evt.type]) {
						handler(evt);
					}
				}
				return true;
			}
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
