using PathCreation.Examples;
using UnityEngine;
[System.Serializable]
public struct EncounterStruct
{
    public EnemyManager enemyManager;
    public int enemyCount;
    public int minComboLength, maxComboLength;
}
public class EncounterManager : MonoBehaviour
{
    public EncounterStruct[] Encounters;
    private int encounterIndex;
    public static EncounterManager instance;
    public PathFollower playerPathFollower;
    public float defaultEnemyAggroDelay = 2f;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        encounterIndex = 0;
        StartEncounter(defaultEnemyAggroDelay);
    }

    public void GoToNextEncounter()
    {
        Debug.Log("All enemies dead, proceeding to next encounter"); 
        // Trigger player camera trip
        
        StartEncounter(defaultEnemyAggroDelay);
        
        // jacob i need u
    }

    /// <summary>
    /// Starts encounter at encounter index and increments
    /// </summary>
    private void StartEncounter(float enemyAggroDelay)
    {
        if (encounterIndex > Encounters.Length - 1)
            return;
        
        int count = Encounters[encounterIndex].enemyCount;
        int minLegnth = Encounters[encounterIndex].minComboLength;
        int maxLength = Encounters[encounterIndex].maxComboLength;
        Encounters[encounterIndex].enemyManager.InitializeEncounter(count, minLegnth, maxLength, enemyAggroDelay);
        
        encounterIndex++;
    }
}
