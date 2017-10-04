using System;

using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

namespace SharpBCI {

	/**
	 * An object which configures a SharpBCI object.
	 * Generally used internally w/in SharpBCIBuilder
	 * @see SharpBCIBuilder
	 */
	public class SharpBCIConfig {
		public EEGDeviceAdapter adapter;
		public string pipelineFile;
		public Dictionary<string, object> stageScope;
	}

	/**
	 * A builder class for SharpBCIConfig
	 * @see SharpBCIConfig
	 * @see SharpBCI
	 */
	public class SharpBCIBuilder {
		readonly SharpBCIConfig config = new SharpBCIConfig();

		public SharpBCIBuilder EEGDeviceAdapter(EEGDeviceAdapter adapter) {
			config.adapter = adapter;
			return this;
		}

		public SharpBCIBuilder PipelineFile(string configFile) {
			config.pipelineFile = configFile;
			return this;
		}

		public SharpBCIBuilder AddToPipelineScope(string key, object obj) {
			if (config.stageScope == null)
				config.stageScope = new Dictionary<string, object>();
			config.stageScope.Add(key, obj);
			return this;
		}

		public SharpBCI Build() {
				return new SharpBCI(config);
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
	 * This is the "main" class which you should create.
	 */
	public class SharpBCI {

		public const string SCOPE_ADAPTER_KEY = "SharpBCI_Adapter";
		public const string SCOPE_SHARP_BCI_KEY = "SharpBCI_Instance";
		public const string SCOPE_CHANNELS_KEY = "SharpBCI_Channels";
		public const string SCOPE_SAMPLE_RATE_KEY = "SharpBCI_SampleRate";

		public static readonly string[] SCOPE_RESERVED_KEYWORDS = { 
			SCOPE_ADAPTER_KEY,
			SCOPE_SHARP_BCI_KEY,
			SCOPE_CHANNELS_KEY,
			SCOPE_SAMPLE_RATE_KEY,
		};

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

		//public variables

		/**
		 * How many channels the EEGDeviceAdapter has
		 */
		public readonly int channels;

		/**
		 * Nominal sample rate of EEGDeviceAdapter, used for FFT and to understand EEGEvents
		 */
		public readonly double sampleRate;

		/**
		 * Is the device connected to a human
		 * Based on the Muse EEG status updates: 
		 * @returns 4 = no connection, 2 = ok connection, 1 = good connection, 3 = unused, complain to Muse about that
		 */
		public double[] connectionStatus { get { return _connectionStatus; } }

		// end public variables

		// readonlys
		readonly EEGDeviceAdapter adapter;

		readonly IPipeable[] stages;

		readonly Dictionary<EEGDataType, List<SharpBCIRawHandler>> rawHandlers = new Dictionary<EEGDataType, List<SharpBCIRawHandler>>();
		readonly Dictionary<int, List<SharpBCITrainedHandler>> trainedHandlers = new Dictionary<int, List<SharpBCITrainedHandler>>();

		readonly TaskFactory taskFactory;
		readonly CancellationTokenSource cts;

		// IPipeables to train on.
		readonly IPredictor[] predictors;
		// end readonlys

		// variables
		double[] _connectionStatus;
		// end variables

		/**
         * @param config a valid config object, generally built with SharpBCIBuilder
         * @see SharpBCIConfig
         * @see SharpBCIBuilder
		 */
		public SharpBCI(SharpBCIConfig config) {
			Logger.Log("SharpBCI started");

			// begin check args
			if (config.adapter == null)
				throw new ArgumentException("config.adapter must not be null");

			if (config.adapter.channels <= 0)
				throw new ArgumentException("config.channels must be > 0");

			if (config.pipelineFile == null)
				throw new ArgumentException("config.pipelineFile must not be null and must be valid file name");

			if (config.stageScope == null)
				config.stageScope = new Dictionary<string, object>();

			foreach (var key in SCOPE_RESERVED_KEYWORDS) {
				if (config.stageScope.ContainsKey(key))
					throw new ArgumentException(string.Format("{0} is a reserved stage scope keyword", key)); 
			}
			// end check args

			// begin state config
			adapter = config.adapter;
			channels = adapter.channels;
			sampleRate = adapter.sampleRate;
			_connectionStatus = new double[channels];
			for (int i = 0; i < channels; i++) {
				_connectionStatus[i] = 4;
			}
			// end state config

			// kinda a kludge: link up connection status output, flush is called in EEGDeviceProducer
			adapter.AddHandler(EEGDataType.CONTACT_QUALITY, UpdateConnectionStatus);

			// begin internal pipeline construction
			var scope = config.stageScope;
			scope.Add(SCOPE_SHARP_BCI_KEY, this);
			scope.Add(SCOPE_ADAPTER_KEY, adapter);
			scope.Add(SCOPE_CHANNELS_KEY, channels);
			scope.Add(SCOPE_SAMPLE_RATE_KEY, sampleRate);

			stages = PipelineSerializer.CreateFromFile(config.pipelineFile, scope);
			var predictorsList = new List<IPredictor>();
			foreach (var stage in stages) {
				if (stage is IPredictor)
					predictorsList.Add((IPredictor) stage);
			}

			if (predictorsList.Count == 0)
				throw new ArgumentException("Pipeline does not implement any IPredictors");

			predictors = predictorsList.ToArray();
			// end internal pipeline construction

			// begin start associated threads & EEGDeviceAdapter
			cts = new CancellationTokenSource();
			taskFactory = new TaskFactory(cts.Token, 
				TaskCreationOptions.LongRunning, 
				TaskContinuationOptions.None, 
				TaskScheduler.Default
			);

			foreach (var stage in stages) {
				stage.Start(taskFactory, cts);
			}
			// end start associated threads & EEGDeviceAdapter
		}

		/**
		 * Start training SharpBCI on the EEG data from now on
		 * Should be paired w/ a StopTraining(id) call
		 * @param id - a unique non-negative non-zero integer which identifies this trained event
		 */
		public void StartTraining(int id) {
			if (id <= 0) throw new ArgumentException("Training id invalid");

			foreach (var predictor in predictors) {
				predictor.StartTraining(id);
			}
		}

		/**
		 * Stop training SharpBCI on the current trainingID
         * @param id - a unique non-negative non-zero integer which identifies this trained event
		 */
		public void StopTraining(int id) {
			if (id <= 0) throw new ArgumentException("Training id invalid");

			foreach (var predictor in predictors) {
				predictor.StopTraining(id);
			}
		}

		public void AddTrainedHandler(int id, SharpBCITrainedHandler handler) { 
			if (id <= 0) throw new ArgumentException("Training id invalid");
			lock (trainedHandlers) {
				if (!trainedHandlers.ContainsKey(id))
					trainedHandlers.Add(id, new List<SharpBCITrainedHandler>());
				trainedHandlers[id].Add(handler);
			}
		}

		public void RemoveTrainedHandler(int id, SharpBCITrainedHandler handler) { 
			if (id <= 0) throw new ArgumentException("Training id invalid");
			lock (trainedHandlers) {
				if (!trainedHandlers.ContainsKey(id))
					throw new ArgumentException("No handlers registered for id: " + id);
				if (!trainedHandlers[id].Remove(handler))
					throw new ArgumentException("Handler '" + handler + "' not registered for id: " + id);
			}
		}

		internal void EmitTrainedEvent(TrainedEvent evt) { 
			lock (trainedHandlers) {
				if (!trainedHandlers.ContainsKey(evt.id))
					return;
				foreach (var handler in trainedHandlers[evt.id]) {
					try {
						handler(evt);
					} catch (Exception e) {
						Logger.Error("Handler " + handler + " encountered exception: " + e);
					}
				}
			}
		}

		public void AddRawHandler(EEGDataType type, SharpBCIRawHandler handler) {
			if (handler == null)
				throw new ArgumentException("handler cannot be null");
			lock (rawHandlers) {
				if (!rawHandlers.ContainsKey(type))
					rawHandlers.Add(type, new List<SharpBCIRawHandler>());
				rawHandlers[type].Add(handler);
			}
		}

		public void RemoveRawHandler(EEGDataType type, SharpBCIRawHandler handler) {
			if (handler == null)
				throw new ArgumentException("handler cannot be null");
			lock (rawHandlers) {
				if (!rawHandlers.ContainsKey(type))
					throw new ArgumentException("No handlers registered for type: " + type);
				if (!rawHandlers[type].Remove(handler))
					throw new ArgumentException("Handler '" + handler + "' not registered for EEGDataType: " + type);
			}
		}

		internal void EmitRawEvent(EEGEvent evt) { 
			lock (rawHandlers) {
				if (!rawHandlers.ContainsKey(evt.type))
					return;

				// Logger.Log("Emitting evt: " + evt.type);
				foreach (var handler in rawHandlers[evt.type]) {
					try {
						handler(evt);
					} catch (Exception e) {
						Logger.Error("Handler " + handler + " encountered exception: " + e);
					}
				}
			}
		}

		/**
		 * Called when all the SharpBCI threads should shutdown.  
		 * You may or may not continue to receive events after calling this.
		 * You should unregister events before calling this: adjust your code accordingly.
		 */
		public void Close() {
			Logger.Log("SharpBCI closed");
			cts.Cancel();
			foreach (var stage in stages) {
				stage.Stop();
			}
		}

		void UpdateConnectionStatus(EEGEvent evt) {
			_connectionStatus = evt.data;
		}
	}

	public class TrainedEventEmitter : Pipeable {
		readonly SharpBCI self;

		public TrainedEventEmitter(SharpBCI self) {
			this.self = self;
		}

		protected override bool Process(object item) {
			TrainedEvent evt = (TrainedEvent)item;
			self.EmitTrainedEvent(evt);
			return true;
		}
	}

	public class RawEventEmitter : Pipeable {
		readonly SharpBCI self;

		public RawEventEmitter(SharpBCI self) {
			this.self = self;
		}

		protected override bool Process(object item) {
			EEGEvent evt = (EEGEvent)item;
			self.EmitRawEvent(evt);
			return true;
		}
	}

	public class EEGDeviceProducer : Pipeable {

		readonly static EEGDataType[] supportedTypes = new EEGDataType[] {
			EEGDataType.EEG,
			//EEGDataType.ALPHA_RELATIVE,
			//EEGDataType.BETA_RELATIVE,
			//EEGDataType.GAMMA_RELATIVE,
			//EEGDataType.DELTA_RELATIVE,
			//EEGDataType.THETA_RELATIVE,
		};

		readonly EEGDeviceAdapter adapter;
		public EEGDeviceProducer(EEGDeviceAdapter adapter) {
			this.adapter = adapter;
		}

		public override void Start(TaskFactory taskFactory, CancellationTokenSource cts) {
			foreach (EEGDataType type in supportedTypes) {
				adapter.AddHandler(type, Add);
			}
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