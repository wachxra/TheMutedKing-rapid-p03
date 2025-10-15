using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public float spawnCooldown = 5f;
    private GameObject currentEnemy;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (currentEnemy == null)
            {
                SpawnEnemy();
            }
            yield return new WaitForSeconds(spawnCooldown);
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoint == null) return;

        currentEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        EnemyController enemy = currentEnemy.GetComponent<EnemyController>();
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
    }
}