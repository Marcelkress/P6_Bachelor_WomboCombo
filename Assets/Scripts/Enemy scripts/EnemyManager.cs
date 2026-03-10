using System.Collections.Generic;
using UnityEngine;

  [System.Serializable]
  public struct startStruct
    {
        public int symbOne;
        public int symbTwo;
    }

public class EnemyManager : MonoBehaviour
{
    public GameObject enemyUnitPrefab;
    [Tooltip("Also determines the enemy count")] public Transform[] spawnPositions;
    public List<startStruct> theFirstUniqueComboStartSteps = new List<startStruct>();
    private List<GameObject> Enemies;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Enemies = new List<GameObject>();

        for (int i = spawnPositions.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (spawnPositions[i], spawnPositions[j]) = (spawnPositions[j], spawnPositions[i]);
        }

        int spawnCount = spawnPositions.Length;
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject enemy = Instantiate(enemyUnitPrefab, spawnPositions[i].position, spawnPositions[i].rotation);

            Enemies.Add(enemy);

            Enemy enemyScript = enemy.GetComponent<Enemy>();
            
            // Først helt random Combo Array for hver enemy
            enemyScript.comboArray = RandomArray();

            // Derefter sikre at de første 2 symboler er unikke for hver enemy
            enemyScript.comboArray[0] = theFirstUniqueComboStartSteps[i].symbOne;
            enemyScript.comboArray[1] = theFirstUniqueComboStartSteps[i].symbTwo;
            
            
            enemyScript.comboArray[0] = theFirstUniqueComboStartSteps[i].symbOne;
            enemyScript.comboArray[1] = theFirstUniqueComboStartSteps[i].symbTwo;
            
        }
    }

    [Header("Random Combo Settings")] 
    [Tooltip("Must be even numbers")]
    public int minLength;
    public int maxLength;

    private int[] RandomArray()
    {
        int lenght = Random.Range(minLength * 2, maxLength * 2); // Ensure the length is even Ganger med 2 for at gøre det mere intuitivt i inspector

        if (lenght % 2 != 0) // Sikrer det er et lige tal
        {
            lenght += 1;
        }

        int[] randArray = new int[lenght];

        return randArray;
    }


}
