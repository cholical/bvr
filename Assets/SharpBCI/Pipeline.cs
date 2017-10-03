using System;

using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections.Concurrent;

namespace SharpBCI {

	public interface IPipeable {
		/**
		 * Set the input on this IPipeable to param input
		 */
		void SetInput(BlockingCollection<object> input);

		/**
		 * Connect the input of other to our output and allow for control of mirror data
		 */
		void Connect(IPipeable other, bool mirror);

		/**
		 * Connect the input of other to our output
		 * Note: mirror == false using this override
		 */
		void Connect(IPipeable other);

		/**
		 * Actually start doing work (i.e., promise to eventually start pushing data to connected pipeables)
		 */
		void Start(TaskFactory taskFactory, CancellationTokenSource cts);

		/**
		 * Require this IPipeable to stop, blocking until actually stopped
		 */
		void Stop();
	}

	public abstract class Pipeable : IPipeable {
		public const int DEFAULT_BUFFER_SIZE = 1000;

		Task runningTask;
		CancellationTokenSource cts;
		CancellationToken token;

		BlockingCollection<object>[] input;

		List<BlockingCollection<object>> allOutputs = new List<BlockingCollection<object>>();

		// TODO do we need to limit the size of this buffer due to memory concerns?
		// BlockingCollection<object> output = new BlockingCollection<object>();

		public void SetInput(BlockingCollection<object> newInput) {
			if (input == null) {
				input = new BlockingCollection<object>[0];
			}
			var inputsAsList = new List<BlockingCollection<object>>(input);
			inputsAsList.Add(newInput);
			input = inputsAsList.ToArray();
		}

		public void Connect(IPipeable other) {
			Connect(other, false);
		}

		public void Connect(IPipeable other, bool mirror) {
			BlockingCollection<object> output;
			if (mirror) {
				output = new BlockingCollection<object>(DEFAULT_BUFFER_SIZE);
				allOutputs.Add(output);
			} else {
				if (allOutputs.Count == 0) {
					allOutputs.Add(new BlockingCollection<object>(DEFAULT_BUFFER_SIZE));
				}
				output = allOutputs[0];
			}
			other.SetInput(output);
		}

		public virtual void Start(TaskFactory taskFactory, CancellationTokenSource cts) {
			this.cts = cts;
			token = cts.Token;
			runningTask = taskFactory.StartNew(Run);
		}

		public virtual void Stop() {
			// TODO do cooperative stopping here, or above us?
			foreach (var o in allOutputs) {
				o.Dispose();
			}
			allOutputs.Clear();
			input = null;
			runningTask.Wait();
		}

		void Run() {
			//Logger.Log("Pipeable " + this + " running");
			try {
				// case: producer
				if (input == null) {
					Logger.Log("Pipeable " + this + " running as producer: nOuputs = " + allOutputs.Count);
					do {
						if (token.IsCancellationRequested) break;
					} while (Process(null));
				}
				// case: consumer (possibly a filter)
				else {
					Logger.Log("Pipeable " + this + " running as consumer/filter: nInputs = " + input.Length + " nOuputs = " + allOutputs.Count);
					while (!token.IsCancellationRequested) {
						object item;
						BlockingCollection<object>.TakeFromAny(input, out item, token);
						if (!Process(item)) break;
					}
				}
			} catch (Exception e) {
				cts.Cancel();
				if (!(e is OperationCanceledException)) {
					Logger.Error("Unexpected exception occurred in Pipeable " + this + ", cancelling pipeline.  Exception: " + e);
					throw;
				}
			} finally {
				foreach (var output in allOutputs) { 
					output.CompleteAdding();
				}
			}
		}

		protected void Add(object item) {
			// TODO this will delay adding if using bounded buffers: explore benefits of spin waiting here
			foreach (var output in allOutputs) {
				output.Add(item, token);
			}
		}

		protected abstract bool Process(object item);
	}
}
