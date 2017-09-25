using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using SharpOSC;
using UnityEngine;

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
		public double[] data;

		public EEGEvent(DateTime timestamp, EEGDataType type, double[] data) {
			this.timestamp = timestamp;
			this.type = type;
			this.data = data;
		}
	}

	public abstract class EEGDeviceAdapter {

		public delegate void DataHandler(EEGEvent evt);

		readonly Dictionary<EEGDataType, List<DataHandler>> handlers = new Dictionary<EEGDataType, List<DataHandler>>();

		readonly Queue<EEGEvent> eventQueue = new Queue<EEGEvent>();

		public abstract void Start();
		public abstract void Stop();

		public void AddHandler(EEGDataType type, DataHandler handler) {
			// Debug.Log("AddHandler type="+type);
			if (!handlers.ContainsKey(type)) {
				handlers.Add(type, new List<DataHandler>());
			}
			handlers[type].Add(handler);
		}

		public void RemoveHandler(EEGDataType type, DataHandler handler) {
			// Debug.Log("RemoveHandler type="+type);
			if (!handlers.ContainsKey(type))
				throw new Exception("Handler was not registered");

			if (!handlers[type].Remove(handler))
				throw new Exception("Handler was not registered");
		}

		public void FlushEvents() {
			lock (eventQueue) {
				// Debug.Log("FlushEvents()");
				while (eventQueue.Count > 0) {
					EEGEvent evt = eventQueue.Dequeue();
					FlushEvent(evt);
				}
			}
		}

		protected void EmitData(EEGDataType type, double[] data) {
			//Debug.Log("EmitData type=" + type);
			lock (eventQueue) {
				//Debug.Log("EmitData lock obtained");
				eventQueue.Enqueue(new EEGEvent(DateTime.UtcNow, type, data));
			}
		}

		private void FlushEvent(EEGEvent evt) {
			if (handlers.ContainsKey(evt.type)) {
				//Debug.Log("FlushEvent type=" + evt.type);
				List<DataHandler> h = handlers[evt.type];
				foreach (DataHandler dh in h) {
					dh(evt);
				}
			}
		}
	}

	public class RemoteOSCAdapter : EEGDeviceAdapter {
		
		int port;

		UDPListener listener;
		Dictionary<string, EEGDataType> typeMap;
		readonly Converter<object, double> converter = new Converter<object, double>(delegate(object inAdd) {
			return (double) inAdd;
		});

		Thread listenerThread;
		bool stopRequested;

		public RemoteOSCAdapter(int port) {
			this.port = port;
		}

		public override void Start() {
			Debug.Log("Starting RemoteOSCAdapter");
			typeMap = InitTypeMap();
			listener = new UDPListener(port);
			listenerThread = new Thread(new ThreadStart(Run));
			listenerThread.Start();
		}

		public override void Stop() {
			Debug.Log("Stopping RemoteOSCAdapter");
			stopRequested = true;
			listenerThread.Join();
			listener.Dispose();
		}

		Dictionary<string, EEGDataType> InitTypeMap() {
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

		void Run() {
			while (!stopRequested) {
				var packet = listener.Receive();
				if (packet != null)
					OnOSCMessageReceived(packet);
			}
		}

		void OnOSCMessageReceived(OscPacket packet) {
			var msg = (OscMessage) packet;
			if (!typeMap.ContainsKey(msg.Address))
				return;

			//Debug.Log("Got packet from: " + msg.Address);
			//Debug.Log("Arguments: ");
			//foreach (var a in msg.Arguments) {
			//	Debug.Log(a.ToString());
			//}

			var data = msg.Arguments.ConvertAll<double>(converter).ToArray();
			var type = typeMap[msg.Address];

			//Debug.Log("EEGType: " + type);
			//Debug.Log("Converted Args: ");
			//foreach (float d in data) {
			//	Debug.Log(d.ToString());
			//}

			EmitData(type, data);
		}
	}

}

