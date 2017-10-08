
namespace SharpBCI {
	public interface IPredictorPipeable : IPipeable {
		void StartTraining(int id);
		void StopTraining(int id);
	}
}
