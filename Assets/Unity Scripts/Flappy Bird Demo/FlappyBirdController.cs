using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpBCI;

/**
 * Controls movement of the camera motion throughout the game world
 */
public class FlappyBirdController : MonoBehaviour {

	//Assigning numeric values to up and down
	public static int UP_ID = 1;
	public static int DOWN_ID = 2;

	public float trainingTime = 30;
	public float upForce = 1;
	public float maxY = 5f;

	//Control display what is shown to the user within game
	public GameObject trainingStatus;
	public GameObject upPrompt;
	public GameObject downPrompt;

	//Install the controllers for use
	public SteamVR_TrackedObject leftController;
	public SteamVR_TrackedObject rightController;

	//Audio sound for making "Ouch" sound when user hits the ceiling
	public AudioClip ouchSound;

	SteamVR_Controller.Device LeftDevice {
		get {
			return SteamVR_Controller.Input((int)leftController.index);
		}
	}

	SteamVR_Controller.Device RightDevice {
		get {
			return SteamVR_Controller.Input((int)rightController.index);
		}
	}

	public bool IsTraining { get { return isTraining; } }

	public float UpPercent { get { return _upTrainedTime / trainingTime; } }

	public float DownPercent { get { return _downTrainedTime / trainingTime; } }

	bool isTraining = true;
	bool trainingUp;
	bool trainingDown;

	float _upTrainedTime;
	float _downTrainedTime;

	//Vector3 startPos;

	bool upQueued;
	bool downQueued;

	Rigidbody rigidBody;

	float started;

	// Use this for initialization
	void Start () {
		// kill old data (if any)
		SharpBCIController.BCI.ClearTrainingData();

		// rest  of setup
		Camera.main.transform.localPosition = Vector3.zero;
		started = Time.time;
		trainingStatus.SetActive(true);
		rigidBody = GetComponent<Rigidbody>();
		//rigidBody.freezeRotation = true;
		//rigidBody.useGravity = false;
		rigidBody.isKinematic = true;
		gameObject.AddComponent<PlayerMover>();
		//startPos = transform.position;
	}

	//int lastSignal = -1;
	// Update is called once per frame
	void Update () {
		if (isTraining) {
			UpdateTraining();
		} else {
			//if (transform.position.y < 2 && lastSignal != 0) {
			//	Debug.Log("Starting up signal");
			//	((InstrumentedDummyAdapter) SharpBCIController.adapter).StartSignal(0);
			//	lastSignal = 0;
			//} else if (transform.position.y > 4 && lastSignal != 1) {
			//	Debug.Log("Starting down signal");
			//	((InstrumentedDummyAdapter) SharpBCIController.adapter).StartSignal(1);
 		//	    lastSignal = 1;	
			//}
		}
	}

	void UpdateTraining() {
		// first update training time percentages until both are successfully completed at 100%
		if (trainingUp) {
			_upTrainedTime += Time.deltaTime;
		} else if (trainingDown) {
			_downTrainedTime += Time.deltaTime;
		}

		//Checks to determine if training is completed. If so, moves on to playing the regular game
		if (isTraining && _upTrainedTime >= trainingTime && _downTrainedTime >= trainingTime) {
			//Debug.Log(trainingUp + " " + trainingDown);
			if (trainingUp) SharpBCIController.BCI.StopTraining(UP_ID);
			if (trainingDown) SharpBCIController.BCI.StopTraining(DOWN_ID);
			isTraining = false;

			Destroy(gameObject.GetComponent<PlayerMover>());
			SharpBCIController.BCI.AddTrainedHandler(UP_ID, OnTrainedEvent);
			SharpBCIController.BCI.AddTrainedHandler(DOWN_ID, OnTrainedEvent);

			//rigidBody.useGravity = true;
			rigidBody.isKinematic = false;

			//Hide training prompts from the screen
			trainingStatus.SetActive(false);
			upPrompt.SetActive(false);
			downPrompt.SetActive(false);

			//Start timining the game, located in Time Controller. Default is 120 seconds.
			TimeController tc = gameObject.AddComponent (typeof(TimeController)) as TimeController;
		}

		//Get training through holding trigger then determine type of training
		if (LeftDevice.GetHairTrigger()) {
			if (trainingUp) {
				upPrompt.SetActive(false);
				//Debug.Log("Stopping up training");
				SharpBCIController.BCI.StopTraining(UP_ID);
				trainingUp = false;
			}
			if (!trainingDown) {
				downPrompt.SetActive(true);
				//((InstrumentedDummyAdapter)SharpBCIController.adapter).StartSignal(1);
				//Debug.Log("Starting down training");
				SharpBCIController.BCI.StartTraining(DOWN_ID);
				trainingDown = true;
			}
		} else if (RightDevice.GetHairTrigger()) {
			if (trainingDown) {
				downPrompt.SetActive(false);
				//Debug.Log("Stopping down training");
				SharpBCIController.BCI.StopTraining(DOWN_ID);
				trainingDown = false;
			}
			if (!trainingUp) {
				upPrompt.SetActive(true);
				//((InstrumentedDummyAdapter)SharpBCIController.adapter).StartSignal(0);
				//Debug.Log("Starting up training");
				SharpBCIController.BCI.StartTraining(UP_ID);
				trainingUp = true;
			}
		} else {
			if (trainingDown) {
				downPrompt.SetActive(false);
				SharpBCIController.BCI.StopTraining(DOWN_ID);
				trainingDown = false;
			} else if (trainingUp) {
				upPrompt.SetActive(false);
				SharpBCIController.BCI.StopTraining(UP_ID);
				trainingUp = false;
			}
		}
	}

	SharpBCI.MovingAverageFilter movementFilter = new SharpBCI.MovingAverageFilter(3);

	void FixedUpdate() {

		if (upQueued) {
			var nextV = (float)movementFilter.Filter(upForce);
			rigidBody.velocity = Vector3.up * nextV;
			upQueued = false;
		} else if (downQueued) {
			var nextV = (float)movementFilter.Filter(-upForce);
			rigidBody.velocity = Vector3.up * nextV;
			downQueued = false;
		}

//		if (upQueued) {
//			Debug.Log ("Performing up");
//			rigidBody.velocity = Vector3.up * upForce;
//			upQueued = false;
//		} else if (downQueued) {
//			Debug.Log ("Performing down");
//			rigidBody.velocity = Vector3.down * upForce;
//			downQueued = false;
//		}
	}

	void OnTrainedEvent(TrainedEvent evt) {
		upQueued = evt.id == UP_ID;
		downQueued = evt.id == DOWN_ID;
	}
}
