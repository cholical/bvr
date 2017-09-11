using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using SharpOSC;

namespace SharpBCI {

	public enum EEGDataType {
		// raw Accelerometer data
		ACCEL,

		// raw EEG data
		EEG,

		// absolute freq bands
		ALPHA_ABSOLUTE,
		BETA_ABSOLUTE,
		GAMMA_ABSOLUTE,
		DELTA_ABSOLUTE,
		THETA_ABSOLUTE,

		// relative freq bands
		ALPHA_RELATIVE,
		BETA_RELATIVE,
		GAMMA_RELATIVE,
		DELTA_RELATIVE,
		THETA_RELATIVE,

		CONTACT_QUALITY,
	}

	public class EEGEvent {
		public DateTime timestamp;
		public EEGDataType type;
		public float[] data;

		public EEGEvent(DateTime timestamp, EEGDataType type, float[] data) {
			this.timestamp = timestamp;
			this.type = type;
			this.data = data;
		}
	}

	public abstract class EEGDeviceAdapter {

		public delegate void DataHandler(EEGEvent evt);

		private Dictionary<EEGDataType, List<DataHandler>> handlers 
		  = new Dictionary<EEGDataType, List<DataHandler>>();

		private Queue<EEGEvent> eventQueue 
		= new Queue<EEGEvent>();

		private Converter<double, float> converter = new Converter<double, float>(delegate (double x) {
			return (float) x;
		});

		public abstract void Start();
		public abstract void Stop();

		public void AddHandler(EEGDataType type, DataHandler handler) {
			if (!handlers.ContainsKey(type)) {
				handlers.Add(type, new List<DataHandler>());
			}
			handlers[type].Add(handler);
		}

		public void RemoveHandler(EEGDataType type, DataHandler handler) {
			if (!handlers.ContainsKey(type))
				throw new Exception("Handler was not registered");

			if (!handlers[type].Remove(handler))
				throw new Exception("Handler was not registered");
		}

		public void FlushEvents() {
			lock (eventQueue) {
				while (eventQueue.Count > 0) {
					EEGEvent evt = eventQueue.Dequeue();
					FlushEvent(evt);
				}
			}
		}

		protected void EmitData(EEGDataType type, List<double> data) {
			lock (eventQueue) {
				eventQueue.Enqueue(new EEGEvent(DateTime.UtcNow, type, data.ConvertAll(converter).ToArray()));
			}
		}

		private void FlushEvent(EEGEvent evt) {
			if (handlers.ContainsKey(evt.type)) {
				List<DataHandler> h = handlers[evt.type];
				foreach (DataHandler dh in h) {
					dh(evt);
				}
			}
		}
	}

	public class RemoteOSCAdapter : EEGDeviceAdapter {

		Thread oscThread;
		int port;
		bool requestStop = false;

		public RemoteOSCAdapter(int port) {
			this.port = port;
		}

		public override void Start() {
			//Console.WriteLine("RemoteOSCAdapter starting");
			oscThread = new Thread(new ThreadStart(Run));
			oscThread.Name = "RemoteOSCAdapterThread";
			oscThread.Start();
		}

		public override void Stop() {
			//Console.WriteLine("RemoteOSCAdapter stopping");
			requestStop = true;
			oscThread.Join();
		}

		private Dictionary<string, EEGDataType> InitTypeMap() {
			Dictionary<string, EEGDataType> typeMap = new Dictionary<string, EEGDataType>();

			// raw EEG data
			typeMap.Add("/muse/eeg", EEGDataType.EEG);
			//typeMap.Add("/muse/eeg/quantization", EEGDataType.QUANTIZATION);

			// accel data
			typeMap.Add("/muse/acc", EEGDataType.ACCEL);

			// absolute power bands
			typeMap.Add("/muse/elements/alpha_absolute", EEGDataType.ALPHA_ABSOLUTE);
			typeMap.Add("/muse/elements/beta_absolute", EEGDataType.BETA_ABSOLUTE);
			typeMap.Add("/muse/elements/gamma_absolute", EEGDataType.GAMMA_ABSOLUTE);
			typeMap.Add("/muse/elements/delta_absolute", EEGDataType.DELTA_ABSOLUTE);
			typeMap.Add("/muse/elements/theta_absolute", EEGDataType.THETA_ABSOLUTE);

			// relative power bands
			typeMap.Add("/muse/elements/alpha_relative", EEGDataType.ALPHA_RELATIVE);
			typeMap.Add("/muse/elements/beta_relative", EEGDataType.BETA_RELATIVE);
			typeMap.Add("/muse/elements/gamma_relative", EEGDataType.GAMMA_RELATIVE);
			typeMap.Add("/muse/elements/delta_relative", EEGDataType.DELTA_RELATIVE);
			typeMap.Add("/muse/elements/theta_relative", EEGDataType.THETA_RELATIVE);

			// session scores
			//typeMap.Add(EEGDataType.ALPHA_SCORE, "/muse/elements/alpha_session_score");
			//typeMap.Add(EEGDataType.BETA_SCORE, "/muse/elements/beta_session_score");
			//typeMap.Add(EEGDataType.GAMMA_SCORE, "/muse/elements/gamma_session_score");
			//typeMap.Add(EEGDataType.DELTA_SCORE, "/muse/elements/delta_session_score");
			//typeMap.Add(EEGDataType.THETA_SCORE, "/muse/elements/theta_session_score");

			// headband status
			typeMap.Add("/muse/elements/horseshoe", EEGDataType.CONTACT_QUALITY);

			// DRL-REF
			// typeMap.Add(EEGDataType.DRL_REF, "/muse/drlref");

			return typeMap;
		}

		public void Run() {
			var listener = new UDPListener(port);
			var converter = new Converter<object, double>(delegate(object inAdd) {
				return (double) inAdd;
			});
			var typeMap = InitTypeMap();
			while (!requestStop) {
				try {
					OscMessage msg = (OscMessage) listener.Receive();
					if (msg != null && typeMap.ContainsKey(msg.Address)) {
						List<double> data = msg.Arguments.ConvertAll<double>(converter);
						EEGDataType type = typeMap[msg.Address];
						EmitData(type, data);
					}
				} catch (Exception e) {
					// TODO thread-safe exception handling?
				}
			}
			listener.Dispose();
		}
	}

}

