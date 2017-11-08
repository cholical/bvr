using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSpawner : MonoBehaviour {

	public GameObject terrainPrefab;
	public GameObject coinPrefab;
	public int nSpawn = 5;
	public float moveSpeed = 1;

	Vector3 terrainSize;

	readonly LinkedList<GameObject> terrainList = new LinkedList<GameObject>();
	readonly LinkedList<GameObject> coinList = new LinkedList<GameObject>();

	// Use this for initialization
	void Start () {
		UnityEngine.VR.InputTracking.disablePositionalTracking = true;
		//offset = transform.position;
		terrainSize = terrainPrefab.GetComponent<Terrain>().terrainData.size;
		for (int i = -nSpawn; i < nSpawn; i++) {
			var pos = Vector3.forward * i * terrainSize.z;
			var t = Instantiate(terrainPrefab, pos, Quaternion.Euler(Vector3.zero));
			terrainList.AddLast(t);
		}

		bool stopTest = true;
		for(int i = 0; i < 200; i++) {
			var random2 = Random.Range (0, 5);
			float xCord = 13.25f;
			float yCord = 2 + random2;
			float zCord = 0 + (i*5);
			var newCoin = Instantiate(coinPrefab, new Vector3(xCord, yCord, zCord), Quaternion.identity);
			coinList.AddLast (newCoin);
		}
	}
	
	// Update is called once per frame
	void Update () {
		foreach (var terrain in terrainList) {
			terrain.transform.position += Vector3.back * moveSpeed * Time.deltaTime;
		}

		foreach (var coin in coinList) {
			coin.transform.position += Vector3.back * moveSpeed * Time.deltaTime;
		}

		var backTerrain = terrainList.First.Value;
		var distFromBack = Mathf.Abs(backTerrain.transform.position.z);
		if (distFromBack > nSpawn * terrainSize.z) {
			//Debug.Log("Removing old terrain and spawning new terrain");
			terrainList.RemoveFirst();
			Destroy(backTerrain);
			var forwardTerrain = terrainList.Last.Value;
			var newPos = forwardTerrain.transform.position + Vector3.forward * terrainSize.z;
			var t = Instantiate(terrainPrefab, newPos, Quaternion.Euler(Vector3.zero));
			terrainList.AddLast(t);
		}
	}

	void OnDestroy () {
		UnityEngine.VR.InputTracking.disablePositionalTracking = false;
	}
}
