using System;

namespace SharpBCI {

	/**
	 * What type of EEG data this EEGEvent represents.
	 * IMPORTANT note to devs: not assigning a value to each field in this enum can break scenes that rely on it.
	 */
	public enum EEGDataType {
		// raw EEG data
		EEG = 0,

		// raw FFT data
		FFT_RAW = 1,
		// smoothed FFT data
		FFT_SMOOTHED = 2,

		// absolute freq bands
		ALPHA_ABSOLUTE = 3,
		BETA_ABSOLUTE = 4,
		GAMMA_ABSOLUTE = 5,
		DELTA_ABSOLUTE = 6,
		THETA_ABSOLUTE = 7,

		// relative freq bands
		ALPHA_RELATIVE = 8,
		BETA_RELATIVE = 9,
		GAMMA_RELATIVE = 10,
		DELTA_RELATIVE = 11,
		THETA_RELATIVE = 12,

		CONTACT_QUALITY = 13,
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
