using System;
using System.IO;
using System.Collections.Generic;

namespace SharpBCI {

	/**
	 * Internal enum to indicate log level
	 * INFO = normal running information
	 * WARNING = possible error, but not fatal
	 * ERROR = fatal error occurred
	 */
	public enum LogLevel {
		INFO,
		WARNING,
		ERROR,
	};

	public interface ILogOutput : IDisposable {
		void Log(LogLevel level, object message);
	}

	public class FileLogger : ILogOutput {

		readonly StreamWriter outputStream;

		public FileLogger(string logName) {
			outputStream = new StreamWriter(logName, true);
			outputStream.WriteAsync("\n\n\n");
		}

		public void Dispose() {
			outputStream.Flush();
			outputStream.Dispose();
		}

		public void Log(LogLevel level, object message) {
			outputStream.WriteLineAsync(string.Format("{0} - [{1}]: {2}", DateTime.Now, level, message));
		}
	}

	public static class Logger {
		
		readonly static List<ILogOutput> outputs = new List<ILogOutput>();

		public static void AddLogOutput(ILogOutput logOutput) {
			outputs.Add(logOutput);
		}

		public static void Dispose() {
			foreach (var o in outputs) {
				o.Dispose();
			}
		}

		public static void Log(object message) {
			_Log(LogLevel.INFO, message);
		}

		public static void Warning(object message) {
			_Log(LogLevel.WARNING, message);
		}

		public static void Error(object message) {
			_Log(LogLevel.ERROR, message);
		}

		static void _Log(LogLevel level, object message) {
			foreach (var o in outputs) {
				o.Log(level, message);
			}
		}
	}
}
