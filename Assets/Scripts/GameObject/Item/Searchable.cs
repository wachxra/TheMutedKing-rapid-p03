/*using UnityEngine;
using System.Collections.Generic;

public class Searchable : MonoBehaviour, IInteractable
{
    public enum SearchMode
    {
        FixedCount,
        Chance
    }

    [Header("Item Pool (Prefabs or ScriptableObjects)")]
    public List<GameObject> possibleItems;
    public Transform[] spawnPoints;

    [Header("Search Settings")]
    public int maxSearches = 3;
    public SearchMode searchMode = SearchMode.FixedCount;

    [Tooltip("ใช้เมื่อ SearchMode = FixedCount")]
    public int maxSpawns = 1;

    [Tooltip("ใช้เมื่อ SearchMode = Chance (0-1)")]
    [Range(0f, 1f)]
    public float chanceToSpawn = 0.5f;

    public bool allowDuplicates = true;

    private int currentSearchCount = 0;
    private int currentSpawnCount = 0;
    private List<GameObject> remainingItems;

    private void Awake()
    {
        if (!allowDuplicates)
            remainingItems = new List<GameObject>(possibleItems);
    }

    public void OnInteract(PlayerController player)
    {
        if (currentSearchCount >= maxSearches)
        {
            Debug.Log("No more searches allowed.");
            return;
        }

        currentSearchCount++;

        bool shouldSpawn = false;

        if (searchMode == SearchMode.FixedCount)
        {
            if (currentSpawnCount < maxSpawns)
                shouldSpawn = true;
        }
        else if (searchMode == SearchMode.Chance)
        {
            shouldSpawn = (Random.value <= chanceToSpawn);
        }

        if (shouldSpawn && possibleItems.Count > 0)
        {
            GameObject itemToSpawn = null;

            if (allowDuplicates)
            {
                int index = Random.Range(0, possibleItems.Count);
                itemToSpawn = possibleItems[index];
            }
            else
            {
                if (remainingItems.Count == 0)
                {
                    Debug.Log("No items left to spawn.");
                    return;
                }

                int index = Random.Range(0, remainingItems.Count);
                itemToSpawn = remainingItems[index];
                remainingItems.RemoveAt(index);
            }

            Transform spawnPoint = spawnPoints.Length > 0 ?
                spawnPoints[Random.Range(0, spawnPoints.Length)] : transform;

            Instantiate(itemToSpawn, spawnPoint.position, Quaternion.identity);
            currentSpawnCount++;

            Debug.Log($"Searched! Item spawned: {itemToSpawn.name}");
        }
        else
        {
            Debug.Log("Searched! But found nothing.");
        }
    }
}*/