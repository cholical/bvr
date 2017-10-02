using UnityEngine;

public class ViveControllerInput : MonoBehaviour {

	public GameObject laserPrefab;
	public GameObject menuPrefab;

	GameObject laser;

	Transform laserTransform;

	Vector3 hitPoint;

	SceneChanger hitObj;

	SteamVR_TrackedObject trackedObj;

	SteamVR_Controller.Device Controller {
		get {
			return SteamVR_Controller.Input((int)trackedObj.index);
		}
	}

	bool menuShown;

	void Awake() {
		trackedObj = GetComponent<SteamVR_TrackedObject>();
	}

	void Start() {
		laser = Instantiate(laserPrefab);
		laserTransform = laser.transform;
	}

	void Update () {
		if (Controller.GetHairTriggerDown()) {
			RaycastHit hit;
			if (Physics.Raycast(trackedObj.transform.position, transform.forward, out hit, 100)) {
				hitPoint = hit.point;
				hitObj = hit.collider.GetComponent<SceneChanger>();
				ShowLaser(hit);
			}
		} else {
			laser.SetActive(false);
			if (hitObj != null) {
				hitObj.ChangeScene();
			}
		}

		if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu)) {
			ToggleMenu();
		}
	}

	void ToggleMenu() {
		if (menuShown) {
			// hide
			menuPrefab.SetActive(false);
		} else {
			// show
			menuPrefab.SetActive(true);
			// TODO do we need to teleport menu?
		}
	}

	void ShowLaser(RaycastHit hit) {
		laser.SetActive(true);
		laserTransform.position = Vector3.Lerp(trackedObj.transform.position, hitPoint, .5f);
		laserTransform.LookAt(hitPoint);
		laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, hit.distance);
	}
}
