
namespace SharpBCI {
	public interface IPredictor : IPipeable {
		void StartTraining(int id);
		void StopTraining(int id);
	}
}
