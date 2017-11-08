using System.Linq;
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

	FlappyBirdController fbc;

	// Use this for initialization
	void Start () {
		UnityEngine.XR.InputTracking.disablePositionalTracking = true;
		//offset = transform.position;
		terrainSize = terrainPrefab.GetComponent<Terrain>().terrainData.size;
		for (int i = -nSpawn; i < nSpawn; i++) {
			var pos = Vector3.forward * i * terrainSize.z;
			var t = Instantiate(terrainPrefab, pos, Quaternion.Euler(Vector3.zero));
			terrainList.AddLast(t);
		}

		fbc = GetComponent<FlappyBirdController>();
	}

	bool coinsInitted = false;

	float spawnZ;

	void InitCoins() {
		if (coinsInitted) return;
		for (int i = 0; i < 50; i++) {
			var random2 = Random.Range(0, 5);
			float xCord = 13.25f;
			float yCord = 2 + random2;
			float zCord = 0 + (i * 5);
			var newCoin = Instantiate(coinPrefab, new Vector3(xCord, yCord, zCord), Quaternion.identity);
			coinList.AddLast (newCoin);
		}
		// final spawn pos of coin
		spawnZ = 49 * 5;
		coinsInitted = true;
	}

	// Update is called once per frame
	void Update () {
		// terrain movement
		foreach (var terrain in terrainList) {
			terrain.transform.position += Vector3.back * moveSpeed * Time.deltaTime;
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

		// coin movement
		if (fbc.IsTraining) {
			return;
		}

		// only spawn coins if fbc.IsTraining == false

		if (!coinsInitted) {
			InitCoins();
		}

		var destroyedCoins = new List<GameObject>();
		foreach (var coin in coinList) {
			if (coin == null) {
				destroyedCoins.Add(coin);
			} else {
				coin.transform.position += Vector3.back* moveSpeed * Time.deltaTime;
			}
		}

		// cleanup coins that have been destroyed by CameraCoinCollide
		foreach (var coin in destroyedCoins) {
			coinList.Remove(coin);
		}

		// we want to spawn coins evenly every 5 units, 
		// so check if the forward most coin has is more than 5 away from spawnZ
		if (Mathf.Abs(coinList.Last.Value.transform.position.z - spawnZ) >= 5) {
			var c = Instantiate(coinPrefab, new Vector3(13.25f, 2 + Random.Range(0, 5), spawnZ), Quaternion.identity);
			coinList.AddLast(c);
		}

		// cleanup coins too far from player
		var backCoin = coinList.First.Value;
		if (Mathf.Abs(backCoin.transform.position.z) > nSpawn * terrainSize.z) {
			coinList.RemoveFirst();
			Destroy(backCoin);
		}
	}

	void OnDestroy () {
		UnityEngine.XR.InputTracking.disablePositionalTracking = false;
	}
}
