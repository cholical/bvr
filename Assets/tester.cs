using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SharpBCI
{
	public class tester : MonoBehaviour
	{
		Text text;

		// Use this for initialization
		void Start ()
		{
			text = GetComponent<Text> ();

			double[] x = { 1, 4, 2, 5, 2, 5, 6, 8, 9, 3, 2 };
			double[] y = { 1, 4, 6, 7, 4, 2, 6, 8, 1, 2, 3 };

			KNearestNeighbor kn = new KNearestNeighbor (1);

			kn.AddTrainingData (2, x);
			kn.AddTrainingData (1, y);

			double n = kn.Predict (x);

			text.text = n.ToString ();


		
		}

	}
}
