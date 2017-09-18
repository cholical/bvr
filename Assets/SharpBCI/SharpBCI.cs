using System;

using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

namespace SharpBCI {

	public class SharpBCIConfig {
		public EEGDeviceAdapter adapter;
		public int channels;
	}

	public class SharpBCI {
		readonly EEGDeviceAdapter adapter;
		readonly int channels;

		List<Pipeable<object, object>> stages;

		public SharpBCI(SharpBCIConfig config) {
			// begin check args
			if (config.adapter == null)
				throw new ArgumentException("config.adapter must not be null");

			if (channels <= 0)
				throw new ArgumentException("config.channels must be > 0");
			// end check args

			// begin state config
			adapter = config.adapter;
			channels = config.channels;
			// end state config

			// begin internal pipeline construction
			var producer = new EEGDeviceProducer(adapter);
			stages.Add((Pipeable<object, object>) producer);

			// TODO other stages

			// end internal pipeline construction

			// begin start associated threads & EEGDeviceAdapter
			TaskFactory f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
			CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource();
			producer.Start(f, cts);

			// TODO other stages

			// end start associated threads & EEGDeviceAdapter
		}

		public void Close() {
			foreach (var stage in stages) {
				stage.Stop();
			}
		}
	}

	public class EEGDeviceProducer : Pipeable<object, EEGEvent> {
		readonly EEGDeviceAdapter adapter;
		public EEGDeviceProducer(EEGDeviceAdapter adapter) {
			this.adapter = adapter;
		}

		public override void Start(TaskFactory taskFactory, CancellationTokenSource cts) {
			adapter.AddHandler(EEGDataType.EEG, Add);
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
