using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour {

	public PlayerMover playerMover;
	public FlappyBirdController fbc;

	public GameObject coinPrefab;
	public Transform[] cellPositions;

	public Vector3 coinOffset = Vector3.zero;
	public float coinRange = 5;
	public float coinSpacing = 5;
	public int coinsPerCell = 10;

	readonly LinkedList<GameObject> cells = new LinkedList<GameObject>();
	bool coinsCreated = false;

	// Use this for initialization
	void Start () {
		playerMover.OnPlayerTeleported += MoveCells;
	}

	void Update() {
		if (!coinsCreated && !fbc.IsTraining) {
			for (int i = 0; i<cellPositions.Length; i++) {
				cells.AddLast(CreateCell(cellPositions[i]));
			}
			coinsCreated = true;
		}
	}

	int nextId = 0;

	GameObject CreateCell(Transform cellPos) {
		var cell = new GameObject("Cell " + nextId++);
		cell.transform.parent = cellPos;
		cell.transform.localPosition = Vector3.zero;

		for (int j = 0; j<coinsPerCell; j++) {
			var coin = Instantiate(coinPrefab);
			coin.transform.parent = cell.transform;
			coin.transform.localPosition = coinOffset + new Vector3(0, Random.Range(0, coinRange), j* coinSpacing);
		}
		return cell;
	}

	void OnDestroy() {
		playerMover.OnPlayerTeleported -= MoveCells;
	}

	void MoveCells() {
		if (!coinsCreated) return;
		//Debug.Log("Moving cells");

		// remove last cell
		var oldCell = cells.First.Value;
		cells.RemoveFirst();
		foreach (Transform child in oldCell.transform) {
			Destroy(child.gameObject);
		}
		Destroy(oldCell);

		// teleport all cells forward by playerMover.teleportDist
		int i = 0;
		foreach (var cell in cells) {
			cell.transform.parent = cellPositions[i++];
			cell.transform.localPosition = Vector3.zero;
		}

		// create new cell at the end
		var newCell = CreateCell(cellPositions[i]);
		cells.AddLast(newCell);
	}
}
