using System;

namespace SharpBCI {
	public static class Logger {

		public static bool UseUnity = false;

		public static void Log(object message) {
			if (UseUnity) {
				UnityEngine.Debug.Log(message);
			} else {
				Console.WriteLine(message);
			}
		}

		public static void Error(object message) {
			if (UseUnity) {
				UnityEngine.Debug.LogError(message);
			} else {
				Console.WriteLine(message);
			}
		}
	}
}
