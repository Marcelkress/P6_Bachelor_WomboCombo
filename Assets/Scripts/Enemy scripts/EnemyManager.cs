using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public int enemyCount;
    public GameObject enemyUnitPrefab;
    public Transform[] spawnPositions;
    
    private List<GameObject> Enemies;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Enemies = new List<GameObject>();

        for (int i = spawnPositions.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (spawnPositions[i], spawnPositions[j]) = (spawnPositions[j], spawnPositions[i]);
        }
        
        int spawnCount = Mathf.Min(enemyCount, spawnPositions.Length);
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject enemy = Instantiate(enemyUnitPrefab, spawnPositions[i].position, spawnPositions[i].rotation);
            Enemies.Add(enemy);
        }
    }
}
