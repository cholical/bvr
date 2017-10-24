using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

public class DummyAdapterController : MonoBehaviour {

	public void ChangeSignal(int signal) {
		((InstrumentedDummyAdapter)SharpBCIController.adapter).StartSignal(signal);
	}

}
