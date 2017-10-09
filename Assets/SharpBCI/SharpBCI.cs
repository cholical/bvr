using System;

using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

// note to devs: this will appear on the main page of documentation site
/**
 * \mainpage Welcome to the documentation site for SharpBCI, a robust real-time data processing library for C#.
 * All methods should be assumed to be thread-safe (i.e., atomic) unless otherwise stated in the documentation.
 * \section Getting Started
 * Please read the Manual (//TODO insert link) for more detailed information about getting started.
 * SharpBCI should ideally be created using SharpBCIBuilder, but for flexiblity the "raw" SharpBCIConfig class is also provided
 */


namespace SharpBCI {

	/**
	 * An object which configures a SharpBCI object.
	 * Generally used internally w/in SharpBCIBuilder
	 * @see SharpBCIBuilder
	 */
	public class SharpBCIConfig {
		/**
		 * The device adapter which will be used to get raw data
		 * Must emit EEGDataType.EEG and EEGDataType.CONTACT_QUALITY EEGEvents
		 * @see EEGDeviceAdapter
		 * @see EEGDataType
		 * @see EEGEvent
		 */
		public EEGDeviceAdapter adapter;

		/**
		 * The filename which contains a config file consistent with the format SharpBCI uses for pipeline construction
		 * @see PipelineSerializer
		 */
		public string pipelineFile;

		/**
		 * objects which will be added to the "scope" used by PipelineSerializer during construction
		 * SharpBCI_**** strings are reserved for library use only.
		 * @see PipelineSerializer
		 */
		public Dictionary<string, object> stageScope;
	}

	/**
	 * A builder class for SharpBCIConfig
	 * @see SharpBCIConfig
	 * @see SharpBCI
	 */
	public class SharpBCIBuilder {
		readonly SharpBCIConfig config = new SharpBCIConfig();

		/**
		 * Set the EEGDeviceAdapter that SharpBCI will use, overriding possible calls before
         * @see EEGDeviceAdapter
		 */
		public SharpBCIBuilder EEGDeviceAdapter(EEGDeviceAdapter adapter) {
			config.adapter = adapter;
			return this;
		}

		/**
		 * Set the filename that configures the SharpBCI's pipeline
		 * @see PipelineSerializer
		 */
		public SharpBCIBuilder PipelineFile(string configFile) {
			config.pipelineFile = configFile;
			return this;
		}

		/**
		 * Add the given object to the scope that PipelineSerializer will use when instantiating Pipeables
		 * @see PipelineSerializer
		 */
		public SharpBCIBuilder AddToPipelineScope(string key, object obj) {
			if (config.stageScope == null)
				config.stageScope = new Dictionary<string, object>();
			config.stageScope.Add(key, obj);
			return this;
		}

		/**
		 * Create and return the library given previous configuration calls.  
		 * Possibly raises ArgumentException exceptions if an invalid config was passed
		 * @see SharpBCI
		 */
		public SharpBCI Build() {
				return new SharpBCI(config);
		}
	}

	/**
	 * A generic event which indicates previously trained event occured
	 */
	public class TrainedEvent {
		
		/**
		 * Which trained event was detected
		 */
		public readonly int id;
		/**
		 * When the event was detected
		 */
		public readonly DateTime time;

		public TrainedEvent(int i) {
			id = i;
			time = DateTime.Now;
		}
	}

	/**
	 * This is the "main" class which you should create.
	 * All SharpBCI operates are coordinated by an instance of this class.
	 * The overhead for creating this class is rather large, so it should only be created once per usage.
	 */
	public class SharpBCI {

		/**
		 * None of your scope keys should start with this prefix
		 */
		public const string RESERVED_PREFIX = "SharpBCI";

		/**
		 * Keyword for the EEGDeviceAdapter configured
		 */
		public const string SCOPE_ADAPTER_KEY = RESERVED_PREFIX + "Adapter";

		/**
		 * Keyword for the SharpBCI instance
		 */
		public const string SCOPE_SHARP_BCI_KEY = RESERVED_PREFIX + "Instance";

		/**
		 * Keyword for the number of EEG channels as reported by SharpBCIAdapter
		 */
		public const string SCOPE_CHANNELS_KEY = RESERVED_PREFIX + "Channels";

		/**
		 * Keyword for the sample rate of EEGDataType.EEG as reported by SharpBCIAdapter
		 */
		public const string SCOPE_SAMPLE_RATE_KEY = RESERVED_PREFIX + "SampleRate";

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
		readonly IPredictorPipeable[] predictors;
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

			foreach (var key in config.stageScope.Keys) {
				if (key.StartsWith(RESERVED_PREFIX, StringComparison.InvariantCulture))
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
			var predictorsList = new List<IPredictorPipeable>();
			foreach (var stage in stages) {
				if (stage is IPredictorPipeable)
					predictorsList.Add((IPredictorPipeable) stage);
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

		/**
		 * Add a training handler which is notified when "id" is detected
		 * Important: does not check if "id" has actually been trained upon
		 * @throws ArgumentException when id less than or equal to zero
		 */
		public void AddTrainedHandler(int id, SharpBCITrainedHandler handler) {
			if (id <= 0) throw new ArgumentException("Training id invalid");
			lock (trainedHandlers) {
				if (!trainedHandlers.ContainsKey(id))
					trainedHandlers.Add(id, new List<SharpBCITrainedHandler>());
				trainedHandlers[id].Add(handler);
			}
		}

		/**
		 * Remove a previously added training handler
		 * Important: does not check if "id" has actually been trained upon
         * @throws ArgumentException when id less than or equal to zero or if training handler was not previously added
		 */
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

		/**
		 * Add a handler for raw (i.e, anything in EEGDataType) events
		 * Adding a handler does not guarantee events will actually be recieved, 
		 * this is dependent on the configuration of the pipeline
		 * @throws ArgumentException if handler is null
		 */
		public void AddRawHandler(EEGDataType type, SharpBCIRawHandler handler) {
			if (handler == null)
				throw new ArgumentException("handler cannot be null");
			lock (rawHandlers) {
				if (!rawHandlers.ContainsKey(type))
					rawHandlers.Add(type, new List<SharpBCIRawHandler>());
				rawHandlers[type].Add(handler);
			}
		}

		/**
 		 * Remove a handler for raw(i.e, anything in EEGDataType) events
         * @throws ArgumentException if handler is null
		 */
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
		 * You may or may not continue to receive events after calling this
		 * You should unregister events before calling this to avoid memory leaks
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

	/**
	 * An end-point consumer which emits TrainedEvents it received
	 * Must only be connected to Pipeables which output TrainedEvents
	 */
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

	/**
	 * An end-point consumer which emits TrainedEvents it received
	 * Must only be connected to Pipeables which output EEGEvents
	 */
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

	/**
	 * Wraps an EEGDeviceAdapter with a Pipeable and emits EEGDataType.EEG events
	 * Should only be used as a producer not consumer
	 */
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