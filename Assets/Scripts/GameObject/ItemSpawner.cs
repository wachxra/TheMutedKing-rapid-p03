using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SpawnGroup
{
    public string groupName = "New Spawn Group";

    [Header("Item & Location")]
    [Tooltip("Prefab ของที่จะสุ่ม spawn")]
    public GameObject itemPrefab;

    [Tooltip("รายการตำแหน่งที่สามารถ spawn ได้")]
    public List<Transform> spawnPoints;

    [Header("Spawn Count")]
    [Tooltip("จำนวนต่ำสุดที่จะสุ่มเกิดในรอบนั้น ๆ")]
    [Range(0, 10)] public int minSpawnCount = 1;

    [Tooltip("จำนวนสูงสุดที่จะสุ่มเกิดในรอบนั้น ๆ")]
    [Range(0, 10)] public int maxSpawnCount = 1;

    [HideInInspector]
    public List<GameObject> currentSpawnedItems = new List<GameObject>();
}

public class ItemSpawner : MonoBehaviour
{
    private int currentLevelID = 0;

    [Header("Global Spawn Settings")]
    [Tooltip("รายการกลุ่มการสุ่ม Spawn ทั้งหมด เช่น Grass, Chest, Torch. แต่ละกลุ่มจะถูกสุ่มเกิดแยกกัน")]
    public List<SpawnGroup> spawnGroups;

    void Start()
    {
        TeleportPoint.OnTeleport += HandleTeleport;
        SpawnAllGroups(currentLevelID);
    }

    void OnDestroy()
    {
        TeleportPoint.OnTeleport -= HandleTeleport;
    }

    private void HandleTeleport(int newLevelID)
    {
        currentLevelID = newLevelID;
        DestroyCurrentItems();
        SpawnAllGroups(currentLevelID);
        Debug.Log($"ItemSpawner: Teleport detected. Respawning all items for Level ID: {currentLevelID}.");
    }

    private void DestroyCurrentItems()
    {
        foreach (var group in spawnGroups)
        {
            foreach (var item in group.currentSpawnedItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            group.currentSpawnedItems.Clear();
        }
    }

    public void SpawnAllGroups(int levelID)
    {

        foreach (var group in spawnGroups)
        {
            SpawnGroupItem(group);
        }
    }

    private void SpawnGroupItem(SpawnGroup group)
    {
        if (group.itemPrefab == null)
        {
            Debug.LogWarning($"ItemSpawner: Item Prefab is missing for group {group.groupName}. Skipping.");
            return;
        }

        if (group.spawnPoints == null || group.spawnPoints.Count == 0)
        {
            Debug.LogError($"ItemSpawner: Spawn Points list is empty for group {group.groupName}. Cannot spawn.");
            return;
        }

        int maxPossibleSpawns = group.spawnPoints.Count;
        int minCount = Mathf.Min(group.minSpawnCount, maxPossibleSpawns);
        int maxCount = Mathf.Min(group.maxSpawnCount, maxPossibleSpawns);

        int spawnCount = UnityEngine.Random.Range(minCount, maxCount + 1);

        List<Transform> availablePoints = new List<Transform>(group.spawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePoints.Count == 0)
            {
                Debug.LogWarning($"ItemSpawner: Ran out of unique spawn points for {group.groupName}. Spawned {i} out of {spawnCount} intended.");
                break;
            }

            int randomIndex = UnityEngine.Random.Range(0, availablePoints.Count);
            Transform spawnLocation = availablePoints[randomIndex];

            GameObject spawnedItem = Instantiate(group.itemPrefab, spawnLocation.position, Quaternion.identity);
            group.currentSpawnedItems.Add(spawnedItem);

            availablePoints.RemoveAt(randomIndex);
        }

        Debug.Log($"ItemSpawner: Group '{group.groupName}' spawned {group.currentSpawnedItems.Count} items from {group.spawnPoints.Count} points.");
    }
}