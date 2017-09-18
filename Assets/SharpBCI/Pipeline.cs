using System;

using System.Threading;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace SharpBCI {

	public interface IPipeable<In, Out> {
		/*
		 * Set the input on this IPipeable to param input
		 */
		void SetInput(BlockingCollection<In> input);

		/*
		 * Connect the input of other to our output
		 */
		void Connect(IPipeable<Out, object> other);

		/*
		 * Actually start doing work (i.e., promise to eventually start pushing data to connected pipeables)
		 */
		void Start(TaskFactory taskFactory, CancellationTokenSource cts);

		/*
		 * Require this IPipeable to stop, blocking until actually stopped
		 */
		void Stop();
	}

	public abstract class Pipeable<In, Out> : IPipeable<In, Out> {
		Task runningTask;
		CancellationTokenSource cts;
		CancellationToken token;

		BlockingCollection<In> input;

		public static explicit operator Pipeable<In, Out>(EEGDeviceProducer v)
		{
			throw new NotImplementedException();
		}

		// TODO do we need to limit the size of this buffer due to memory concerns?
		BlockingCollection<Out> output = new BlockingCollection<Out>();

		public void SetInput(BlockingCollection<In> input) {
			this.input = input;
		}

		public void Connect(IPipeable<Out, object> other) {
			other.SetInput(output);		
		}

		public virtual void Start(TaskFactory taskFactory, CancellationTokenSource cts) {
			this.cts = cts;
			this.token = cts.Token;
			runningTask = taskFactory.StartNew(Run);
		}

		public virtual void Stop() {
			// TODO do cooperative stopping here, or above us?
			runningTask.Wait();
		}

		void Run() {
			try {
				// case: producer
				if (input == null) {
					do {
						if (token.IsCancellationRequested) break;
					} while (Process(default(In)));
				}
				// case: consumer (possibly a filter)
				else {
					foreach (var item in input.GetConsumingEnumerable(token)) {
						if (token.IsCancellationRequested || !Process(item))
							break;
					}
				}
			} catch (Exception e) {
				cts.Cancel();
				if (!(e is OperationCanceledException))
					throw;
			} finally {
				output.CompleteAdding();
			}
		}

		protected void Add(Out item) {
			output.Add(item, token);
		}

		protected abstract bool Process(In item);
	}
}
