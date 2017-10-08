using System;

namespace SharpBCI {

	public enum EEGDataType {
		// raw EEG data
		EEG,

		// raw FFT data
		FFT_RAW,
		// smoothed FFT data
		FFT_SMOOTHED,

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
		public readonly DateTime timestamp;
		public readonly EEGDataType type;
		public readonly double[] data;
		public readonly object extra;

		public EEGEvent(DateTime timestamp, EEGDataType type, double[] data) 
			: this(timestamp, type, data, null) {}

		public EEGEvent(DateTime timestamp, EEGDataType type, double[] data, object extra) {
			this.timestamp = timestamp;
			this.type = type;
			this.data = data;
			this.extra = extra;
		}

		public override string ToString () {
			return string.Format ("EEGEvent({0:T}/{1}/{2}/{3})", timestamp, type, string.Join(" ", data), extra);
		}
	}

}
