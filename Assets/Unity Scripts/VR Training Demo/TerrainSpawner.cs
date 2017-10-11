using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSpawner : MonoBehaviour {

	public GameObject terrainPrefab;
	public int nSpawn = 5;
	public float moveSpeed = 1;

	Vector3 terrainSize;

	readonly LinkedList<GameObject> terrainList = new LinkedList<GameObject>();

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
	}
	
	// Update is called once per frame
	void Update () {
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
	}
}
