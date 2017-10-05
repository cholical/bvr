using UnityEngine;

public class ViveControllerInput : MonoBehaviour {

	public GameObject laserPrefab;
	public GameObject menuPrefab;

	GameObject laser;

	Transform laserTransform;

	//Vector3 hitPoint;

	SceneChanger hitObj;

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
		laser = Instantiate(laserPrefab);
		laserTransform = laser.transform;
	}

	void Update () {
		RaycastHit hit;
		if (Physics.Raycast(trackedObj.transform.position, transform.forward, out hit, 100)) {
			hitObj = hit.collider.GetComponent<SceneChanger>();
			ShowLaser(hit.point, hit.distance);
		} else {
			ShowLaser(trackedObj.transform.position + transform.forward* 100, 100);
		}

		if (Controller.GetHairTriggerDown()) { 
			if (hitObj != null) {
				hitObj.ChangeScene();
			}
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

	void ShowLaser(Vector3 hitPoint, float distance) {
		laser.SetActive(true);
		laserTransform.position = Vector3.Lerp(trackedObj.transform.position, hitPoint, .5f);
		laserTransform.LookAt(hitPoint);
		laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, distance);
	}
}
