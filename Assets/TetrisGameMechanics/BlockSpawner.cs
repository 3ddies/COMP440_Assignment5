using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public List<GameObject> tetrominoPrefabs; // assign root Tetromino prefabs in Inspector
    public Vector3 spawnPosition = new Vector3(5f, 20f, 0f); // adjust to top-center of your grid
    public float spawnDelay = 0.1f;

    private void Start()
    {
        StartCoroutine(SafeSpawnRoutine());
    }

    private IEnumerator SafeSpawnRoutine()
    {
        // Wait until GridManager.Instance exists before spawning
        yield return new WaitUntil(() => GridManager.Instance != null);
        yield return new WaitForEndOfFrame();
        SpawnNextBlock();
    }

    private void SpawnNextBlock()
    {
        if (tetrominoPrefabs == null || tetrominoPrefabs.Count == 0)
        {
            Debug.LogWarning("No tetromino prefabs assigned!");
            return;
        }

        // Pick a random prefab
        GameObject prefab = tetrominoPrefabs[Random.Range(0, tetrominoPrefabs.Count)];

        // Instantiate the root prefab only
        GameObject newBlock = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Subscribe to landing event to spawn the next block
        FallingBlock fb = newBlock.GetComponent<FallingBlock>();
        if (fb != null)
        {
            FallingBlock.OnBlockLanded += HandleBlockLanded;
        }
        else
        {
            Debug.LogWarning("Spawned prefab does not have a FallingBlock component!");
        }
    }

    private void HandleBlockLanded(FallingBlock landedBlock)
    {
        // Unsubscribe safely
        FallingBlock.OnBlockLanded -= HandleBlockLanded;

        // Spawn next piece after a slight delay
        Invoke(nameof(SpawnNextBlock), spawnDelay);
    }
}
