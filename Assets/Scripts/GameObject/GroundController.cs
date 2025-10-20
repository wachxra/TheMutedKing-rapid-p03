using UnityEngine;
using System.Collections.Generic;

public class GroundController : MonoBehaviour
{
    public static GroundController Instance;

    [Header("Ground Settings")]
    public GameObject groundPrefab;
    public Transform groundSpawnPoint;
    public float groundLength = 20f;
    public int groundPoolSize = 3;

    [Header("Obstacle Settings")]
    public List<GameObject> obstaclePrefabs;
    public float obstacleSpawnChance = 0.5f;
    public float obstacleYOffset = 1f;
    public float timeBetweenObstacleSpawns = 2.0f;

    [Header("Trigger Points (for Dynamic Control)")]
    public Transform obstacleDestroyPoint;
    public Transform wallSpawnPoint;
    public Transform wallTargetPoint;

    [Header("Anti-Backtracking Wall")]
    public GameObject antiBacktrackWallPrefab;
    public GameObject currentWall;
    public float wallReturnSpeed = 5f;

    [Header("Obstacle Spawn Point")]
    public Transform obstacleSpawnPoint;

    private List<GameObject> activeGrounds = new List<GameObject>();
    private float timeUntilNextObstacleSpawn = 0f;
    public float currentMoveSpeed = 0f;
    private float lastGroundPositionX;
    private float wallFollowDistance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (wallSpawnPoint == null)
        {
            GameObject point = new GameObject("AutoWallSpawnPoint");
            point.transform.position = new Vector3(-10f, groundSpawnPoint.position.y + 2f, 0f);
            wallSpawnPoint = point.transform;
        }

        lastGroundPositionX = groundSpawnPoint.position.x;
        for (int i = 0; i < groundPoolSize; i++)
        {
            SpawnNewGround();
        }
        timeUntilNextObstacleSpawn = timeBetweenObstacleSpawns;

        SpawnWall();
    }

    void Update()
    {
        float distance = currentMoveSpeed * Time.deltaTime;

        MoveEverything(distance);
        HandleTimedObstacleSpawn();
        HandleObstacleCleanup();

        if (activeGrounds.Count > 0)
        {
            if (activeGrounds[0] != null && activeGrounds[0].transform.position.x < -groundLength)
            {
                RecycleGround();
                SpawnNewGround();
            }
        }

        MoveWall(distance);
    }

    public void SetMovement(float speed)
    {
        currentMoveSpeed = speed;
    }

    public void SpawnWall()
    {
        if (currentWall == null && antiBacktrackWallPrefab != null && wallSpawnPoint != null)
        {
            Vector3 spawnPos = wallSpawnPoint.position;
            currentWall = Instantiate(antiBacktrackWallPrefab, spawnPos, Quaternion.identity, transform);

            if (PlayerController.Instance != null)
                wallFollowDistance = currentWall.transform.position.x - PlayerController.Instance.transform.position.x;
        }
    }

    private void MoveWall(float distance)
    {
        if (currentWall == null || PlayerController.Instance == null) return;

        Transform player = PlayerController.Instance.transform;
        float step = wallReturnSpeed * Time.deltaTime;

        if (PlayerController.Instance.moveInput < 0)
        {
            float targetX = Mathf.Min(player.position.x - 1f, currentWall.transform.position.x);
            Vector3 targetPos = new Vector3(targetX, currentWall.transform.position.y, currentWall.transform.position.z);
            currentWall.transform.position = Vector3.MoveTowards(currentWall.transform.position, targetPos, step);
        }
        else if (PlayerController.Instance.moveInput > 0)
        {
            if (wallTargetPoint != null)
            {
                if (currentWall.transform.position.x < wallTargetPoint.position.x)
                {
                    Vector3 targetPos = new Vector3(wallTargetPoint.position.x, currentWall.transform.position.y, currentWall.transform.position.z);
                    currentWall.transform.position = Vector3.MoveTowards(currentWall.transform.position, targetPos, step);
                    wallFollowDistance = wallTargetPoint.position.x - player.position.x;
                }
                else
                {
                    Vector3 targetPos = new Vector3(player.position.x + wallFollowDistance, currentWall.transform.position.y, currentWall.transform.position.z);
                    if (targetPos.x > currentWall.transform.position.x)
                        currentWall.transform.position = Vector3.MoveTowards(currentWall.transform.position, targetPos, step);
                }
            }
        }
    }

    private void HandleTimedObstacleSpawn()
    {
        if (currentMoveSpeed != 0f)
        {
            GameObject[] existingObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
            if (existingObstacles.Length > 0) return;

            if (timeUntilNextObstacleSpawn > 0) timeUntilNextObstacleSpawn -= Time.deltaTime;
            else
            {
                if (obstaclePrefabs.Count > 0)
                {
                    int randomIndex = Random.Range(0, obstaclePrefabs.Count);
                    GameObject obstaclePrefab = obstaclePrefabs[randomIndex];

                    Vector3 spawnPos = obstacleSpawnPoint != null
                        ? obstacleSpawnPoint.position
                        : new Vector3(lastGroundPositionX, groundSpawnPoint.position.y + obstacleYOffset, 0f);

                    GameObject newObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, transform);
                    newObstacle.tag = "Obstacle";
                }

                timeUntilNextObstacleSpawn = timeBetweenObstacleSpawns;
            }
        }
    }

    private void HandleObstacleCleanup()
    {
        if (obstacleDestroyPoint == null) return;

        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obstacle in obstacles)
        {
            if (obstacle != null && obstacle.transform.position.x < obstacleDestroyPoint.position.x)
                Destroy(obstacle);
        }
    }

    private void SpawnNewGround()
    {
        if (groundPrefab == null) return;

        float newGroundX = lastGroundPositionX + groundLength;
        GameObject newGround = Instantiate(groundPrefab, new Vector3(newGroundX, groundSpawnPoint.position.y, 0), Quaternion.identity, transform);
        activeGrounds.Add(newGround);
        lastGroundPositionX = newGroundX;

        if (Random.value < obstacleSpawnChance && obstaclePrefabs.Count > 0)
            SpawnObstacle(newGround.transform);
    }

    private void RecycleGround()
    {
        if (activeGrounds.Count == 0) return;

        GameObject oldGround = activeGrounds[0];
        activeGrounds.RemoveAt(0);
        Destroy(oldGround);
    }

    private void MoveEverything(float distance)
    {
        foreach (GameObject ground in activeGrounds)
        {
            if (ground != null) ground.transform.Translate(Vector3.left * distance);
        }

        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obstacle in obstacles)
        {
            if (obstacle != null && obstacle.activeSelf) obstacle.transform.Translate(Vector3.left * distance);
        }

        if (currentWall != null)
            currentWall.transform.Translate(Vector3.left * distance);
    }

    private void SpawnObstacle(Transform groundTransform)
    {
        if (obstaclePrefabs.Count == 0) return;

        int randomIndex = Random.Range(0, obstaclePrefabs.Count);
        GameObject obstaclePrefab = obstaclePrefabs[randomIndex];
        Vector3 spawnPos = new Vector3(groundTransform.position.x, groundSpawnPoint.position.y + obstacleYOffset, 0f);

        GameObject newObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, transform);
        newObstacle.tag = "Obstacle";
    }
}