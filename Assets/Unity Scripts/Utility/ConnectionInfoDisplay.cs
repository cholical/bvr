using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ConnectionInfoDisplay : MonoBehaviour {

	public Color badColor = Color.red;
	public Color goodColor = Color.green;
	//public int averageSize = 100;

	public float currentQuality { get { return _currentQuality; } }

	Image _image;
	//Queue<int> _samples = new Queue<int>();
	float _currentQuality = 0.0f;

	// Use this for initialization
	void Start () {
		_image = GetComponent<Image>();
		_currentQuality = 0.0f;
	}

	public void AddContactQuality(int quality) {
		_currentQuality = quality;
		// transform from [1, 4] to [0, 1]
		float lerped = (_currentQuality - 1.0f) / 3.0f;
		_image.color = Color.Lerp(goodColor, badColor, lerped);
	}
}
