using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnGroup
{
    public string groupName = "New Enemy Group";
    public GameObject enemyPrefab;
    public List<Transform> spawnPoints = new List<Transform>();
    public int maxEnemiesPerSpawn = 3;
}

public class EnemySpawner : MonoBehaviour
{
    public List<EnemySpawnGroup> spawnGroups = new List<EnemySpawnGroup>();
    public float spawnCooldown = 5f;

    private Dictionary<EnemySpawnGroup, List<GameObject>> currentEnemies = new Dictionary<EnemySpawnGroup, List<GameObject>>();

    void Start()
    {
        foreach (var group in spawnGroups)
        {
            currentEnemies[group] = new List<GameObject>();
        }
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            foreach (var group in spawnGroups)
            {
                currentEnemies[group].RemoveAll(item => item == null);

                int availableSpawn = Mathf.Min(group.maxEnemiesPerSpawn - currentEnemies[group].Count, group.spawnPoints.Count);
                if (availableSpawn > 0)
                {
                    List<Transform> shuffledPoints = new List<Transform>(group.spawnPoints);
                    for (int i = 0; i < shuffledPoints.Count; i++)
                    {
                        int rand = Random.Range(0, shuffledPoints.Count);
                        Transform temp = shuffledPoints[i];
                        shuffledPoints[i] = shuffledPoints[rand];
                        shuffledPoints[rand] = temp;
                    }

                    for (int i = 0; i < availableSpawn; i++)
                    {
                        SpawnEnemy(group, shuffledPoints[i]);
                    }
                }
            }

            yield return new WaitForSeconds(spawnCooldown);
        }
    }

    void SpawnEnemy(EnemySpawnGroup group, Transform point)
    {
        if (group.enemyPrefab == null || point == null) return;

        GameObject enemyObj = Instantiate(group.enemyPrefab, point.position, Quaternion.identity);
        enemyObj.tag = "Enemy";

        EnemyController enemy = enemyObj.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.maxHP = Random.Range(10, 21);
            enemy.currentHP = enemy.maxHP;
            enemy.damage = Random.Range(3, 10);

            enemy.minHitsInCombo = 2;
            enemy.maxHitsInCombo = 6;
            enemy.minTravelTime = 1f;
            enemy.maxTravelTime = 2.5f;
            enemy.attackCooldown = Random.Range(2f, 5f);

            enemy.player = PlayerController.Instance.transform;
        }

        currentEnemies[group].Add(enemyObj);
    }
}