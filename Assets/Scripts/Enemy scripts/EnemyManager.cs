using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

  [System.Serializable]
  public struct startStruct
    {
        public int symbOne;
        public int symbTwo;
    }

public class EnemyManager : MonoBehaviour
{
    public GameObject enemyUnitPrefab, projectileUnityPrefab;
    [Tooltip("Also determines the enemy count")] public Transform[] meleeSpawnPositions;
    public Transform[] projectileSpawnPositions;
    public List<startStruct> theFirstUniqueComboStartSteps = new List<startStruct>();
    public List<GameObject> Enemies;

    /// <summary>
    /// Spawns enemies within specified count and assigns random combos
    /// </summary>
    /// <param name="spawnCount"></param>
    /// <param name="maxComboLength"></param>
    /// <param name="minComboLegnth"></param>
    public void InitializeEncounter(int meleeSpawnCount, int projectileSpawnCount, int maxComboLength, int minComboLegnth, float enemyAggroDelay)
    {
        Enemies = new List<GameObject>();

        for (int i = meleeSpawnPositions.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (meleeSpawnPositions[i], meleeSpawnPositions[j]) = (meleeSpawnPositions[j], meleeSpawnPositions[i]);
        }
        
        for (int i = 0; i < meleeSpawnCount; i++)
        {
            if (i > meleeSpawnPositions.Length - 1)
                return;
            
            GameObject enemy = Instantiate(enemyUnitPrefab, meleeSpawnPositions[i].position, Quaternion.identity);
            Enemies.Add(enemy);

            Enemy enemyScript = enemy.GetComponent<Enemy>();
            enemyScript.manager = this;
            
            // Først helt random Combo Array for hver enemy
            enemyScript.comboArray = RandomArray(minComboLegnth, maxComboLength);
            
            int randNum = Random.Range(1, theFirstUniqueComboStartSteps.Count);

            enemyScript.comboArray[0] = theFirstUniqueComboStartSteps[randNum].symbOne;
            enemyScript.comboArray[1] = theFirstUniqueComboStartSteps[randNum].symbTwo;

            theFirstUniqueComboStartSteps.RemoveAt(randNum);
            
            enemyScript.Initialize(enemyAggroDelay);
        }
        
        for (int i = 0; i < projectileSpawnCount; i++)
        {
            if (i > projectileSpawnPositions.Length - 1)
                return;
            
            GameObject enemy = Instantiate(projectileUnityPrefab, projectileSpawnPositions[i].position, Quaternion.identity);
            Enemies.Add(enemy);

            Enemy enemyScript = enemy.GetComponent<Enemy>();
            enemyScript.manager = this;
            
            // Først helt random Combo Array for hver enemy
            enemyScript.comboArray = RandomArray(minComboLegnth, maxComboLength);
            
            int randNum = Random.Range(1, theFirstUniqueComboStartSteps.Count);

            enemyScript.comboArray[0] = theFirstUniqueComboStartSteps[randNum].symbOne;
            enemyScript.comboArray[1] = theFirstUniqueComboStartSteps[randNum].symbTwo;

            theFirstUniqueComboStartSteps.RemoveAt(randNum);
            
            enemyScript.Initialize(enemyAggroDelay);
        }
    }
    
    private int[] RandomArray(int minLength, int maxLength)
    {
        int length = Random.Range(minLength * 2, maxLength * 2); // Ensure the length is even Ganger med 2 for at gøre det mere intuitivt i inspector
        
        if (length % 2 != 0) // Sikrer det er et lige tal
        {
            length += 1;
        }
        
        int[] randArray = new int[length];
        
        for (int i = 0; i < randArray.Length; i++)
        {
            randArray[i] = Random.Range(1, 3);
        }
        
        return randArray;
    }

    public void ClearAllEntities()
    {
        foreach (var Entities in Enemies)
        {
            Destroy(Entities.gameObject); // både projectiles og enemies 
        }
        Enemies.Clear();
    }
    public void EnemyDied()
    {
        if (Enemies.Count <= 0 && !EncounterManager.instance.bossBattleStarted)
        {
            EncounterManager.instance.GoToNextEncounter();
        }
        else if (Enemies.Count <= 0 && EncounterManager.instance.bossBattleStarted)
        {
            EncounterManager.instance.epicBossLogic.OnMinionsCleared(); 
        }
    }

}
