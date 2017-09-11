using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TogglePanel : MonoBehaviour {

	public string toggleKey;
	public GameObject toggleObject;

	void Update () {
		if (Input.GetButtonDown(toggleKey)) {
			toggleObject.SetActive(!toggleObject.activeSelf);
		}
	}
}
