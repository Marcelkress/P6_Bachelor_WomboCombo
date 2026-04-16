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
    public float enemyAggroDelayVariance = 0.24f; // added variance to enemy aggro delay for more dynamic encounters
    private List<startStruct> pooledComboStartSteps = new List<startStruct>(); // for resetting the theFirstUniqueComboStartSteps list

    public bool enemiesShouldCharge = false;

    
    private void Awake()
    {
        pooledComboStartSteps = new List<startStruct>(theFirstUniqueComboStartSteps); // stores the pool of unique starting combos for resetting laters
    }

    /// <summary>
    /// Spawns enemies within specified count and assigns random combos
    /// </summary>
    /// <param name="spawnCount"></param>
    /// <param name="maxComboLength"></param>
    /// <param name="minComboLegnth"></param>
    public void InitializeEncounter(int meleeSpawnCount, int projectileSpawnCount, int maxComboLength, int minComboLegnth, float enemyAggroDelay)
    {
        // added pga restarting encounter fordi vi havde opbrugt alle unikke comboer
        theFirstUniqueComboStartSteps = new List<startStruct>(pooledComboStartSteps); // resets the list to its original state at the start of each encounter

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
            enemyScript.shouldCharge = enemiesShouldCharge;
            // Først helt random Combo Array for hver enemy
            enemyScript.comboArray = RandomArray(minComboLegnth, maxComboLength);
            
            int randNum = Random.Range(1, theFirstUniqueComboStartSteps.Count);

            enemyScript.comboArray[0] = theFirstUniqueComboStartSteps[randNum].symbOne;
            enemyScript.comboArray[1] = theFirstUniqueComboStartSteps[randNum].symbTwo;

            theFirstUniqueComboStartSteps.RemoveAt(randNum);
            
            float varience = Random.Range(-enemyAggroDelayVariance, enemyAggroDelayVariance);
            enemyScript.Initialize(enemyAggroDelay + varience);
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
            
            float varience = Random.Range(-enemyAggroDelayVariance, enemyAggroDelayVariance);
            enemyScript.Initialize(enemyAggroDelay + varience);
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
            randArray[i] = Random.Range(1, 4); // 1, 2 eller 3 (man skal have 4 selvom vi kun har 3 symboler fordi upper bound er exclusive, som betyder at den ikke kan returnere 4)
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
        Debug.Log(Enemies.Count);
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
