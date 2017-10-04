
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace SharpBCI {

	[DataContract]
	/**
	 * A class which represents the entire pipeline
	 */
	public class SerializedPipeline {
		[DataMember]
		/**
		 * An array of all stages in the pipeline
		 */
		public SerializedStage[] stages;

		[DataMember]
		/**
		 * The connection graph for this pipeline
		 */
		public SerializedConnectionInfo[] stageConnections;
	}

	[DataContract]
	/**
	 * A class which represents a single stage in the pipeline
	 * Can be serialized into a JSON file
	 */
	public class SerializedStage {

		[DataMember]
		public string stageKey;

		[DataMember]
		/**
		 * The full name of an IPipeable class which this SerializedStage represents
		 */
		public string stageClass;

		[DataMember]
		/**
		 * Arguments (should only be primative types) which are supplied to the IPipeable upon instatiation
		 */
		public object[] arguments;
	}

	[DataContract]
	/**
	 * A class which represents a connection graph for the pipeline
	 */
	public class SerializedConnectionInfo {
		[DataMember]
		/**
		 * Which stage object should be used
		 */
		public string stageKey;

		[DataMember]
		/**
		 * Whether or not outputs should be mirrored
		 * @see IPipeable.Connect for details about what this means
		 */
		public bool mirrorOutputs;

		[DataMember]
		/**
		 * An array of SerializedConnectionInfo's which this stage should be connected to upon instatiation
		 */
		public string[] outputs;
	}

	/**
	 * A static class to read SerializedStage files from a file
	 */
	public static class PipelineSerializer {
		
		static readonly DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(SerializedPipeline), new Type[] {
			typeof(SerializedStage),
			typeof(SerializedConnectionInfo)
		});

		/**
		 * Attempt to read a file (in JSON) and create the corresponding set of IPipeable's
		 * The IPipeables have already been connected but not started
		 */
		public static IPipeable[] CreateFromFile(string fileName, Dictionary<string, object> scope) {
			var stream = new System.IO.FileStream(fileName, System.IO.FileMode.Open);
			using (stream) {
				var pipeline = (SerializedPipeline)_serializer.ReadObject(stream);
				var allStages = CreateFromSerialized(pipeline, scope);
				foreach (var node in pipeline.stageConnections) {
					ConnectPipeables(allStages, node);
				}
				return allStages.Values.ToArray();
			}
		}

		static void ConnectPipeables(Dictionary<string, IPipeable> stages, SerializedConnectionInfo node) {
			var stage = stages[node.stageKey];
			foreach (var outKey in node.outputs) {
				var output = stages[outKey];
				stage.Connect(output, node.mirrorOutputs);
			}
		}

		static Dictionary<string, IPipeable> CreateFromSerialized(SerializedPipeline pipeline, Dictionary<string, object> scope) {
			var allStages = new Dictionary<string, IPipeable>();
			foreach (var stage in pipeline.stages) {
				allStages.Add(stage.stageKey, CreatePipeableInstance(stage, scope));
			}
			return allStages;
		}

		static IPipeable CreatePipeableInstance(SerializedStage stage, Dictionary<string, object> scope) {
			var pipeableType = Type.GetType(stage.stageClass, true, false);
			if (!pipeableType.GetInterfaces().Contains(typeof(IPipeable)))
				throw new ArgumentException("stageClass: " + pipeableType.FullName + " must implement SharpBCI.IPipeable");

			for (int i = 0; i < stage.arguments.Length; i++) {
				var arg = stage.arguments[i];
				if (arg is string && scope.ContainsKey(((string)arg))) {
					stage.arguments[i] = scope[(string)arg];
				}
			}

			return (IPipeable)Activator.CreateInstance(pipeableType, stage.arguments);
		}
	}
}
