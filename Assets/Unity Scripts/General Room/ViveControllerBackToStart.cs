using UnityEngine;
using UnityEngine.SceneManagement;

public class ViveControllerBackToStart : MonoBehaviour {

	public GameObject menuPrefab;
	public string sceneName;

	SteamVR_TrackedObject trackedObj;

	SteamVR_Controller.Device Controller {
		get {
			return SteamVR_Controller.Input((int)trackedObj.index);
		}
	}

	bool menuShown = false;

	void Awake() {
		//UnityEngine.VR.InputTracking.disablePositionalTracking = true;
		trackedObj = GetComponent<SteamVR_TrackedObject>();
	}

	void Start() {
		//Run before the first frame
	}

	void Update () {

		if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad)) { 
			SceneManager.LoadScene(sceneName);
		}

		//if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu)) {
		//	ToggleMenu();
		//}
	}

	void ToggleMenu() {
		if (menuPrefab == null) {
			Debug.LogWarning("Menu prefab not set, ignoring ToggleMenu()");
			return;
		}

		if (menuShown) {
			// hide
			menuShown = false;
			menuPrefab.SetActive(false);
		} else {
			// show
			menuShown = true;
			menuPrefab.SetActive(true);
			// TODO do we need to teleport menu?
			//Vector3 headPos = UnityEngine.VR.InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.CenterEye);
			//menuPrefab.transform.position = headPos + 
		}
	}
}
