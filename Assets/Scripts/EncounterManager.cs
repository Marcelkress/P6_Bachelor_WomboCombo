using PathCreation.Examples;
using UnityEngine;
[System.Serializable]
public struct EncounterStruct
{
    public EnemyManager enemyManager;
    public int meleeEnemyCount, projectileEnemyCount;
    public int minComboLength, maxComboLength;
}
public class EncounterManager : MonoBehaviour
{
    public EncounterStruct[] Encounters;
    private int encounterIndex;
    public static EncounterManager instance;
    public PathFollower playerPathFollower;
    public float defaultEnemyAggroDelay = 2f;
    public EpicBossLogic epicBossLogic;

    [Header("Dev Tools")]
    [Tooltip("Set to skip ahead to a specific encounter (0 = normal start)")]
    public bool useDevTools = false;
    public int startFromEncounter = 0;

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
        encounterIndex = Mathf.Clamp(startFromEncounter, 0, Encounters.Length - 1);

        // If skipping ahead, teleport the player to the correct position
        if (encounterIndex > 0 && useDevTools)
        {
            var dest = playerPathFollower.destinations[encounterIndex];
            playerPathFollower.transform.position = dest.transform.position;
            if (dest.lookTarget != null)
            {
                Vector3 dir = dest.lookTarget.position - dest.transform.position;
                dir.y = 0f;
                if (dir != Vector3.zero)
                    playerPathFollower.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        StartEncounter(defaultEnemyAggroDelay);
    }

    public void GoToNextEncounter()
    {
        Debug.Log("All enemies dead, proceeding to next encounter"); 

            // Check if we have finished all regular encounters
            if (encounterIndex >= Encounters.Length)
            {
                Debug.Log("Starting Boss Encounter!");
                if (epicBossLogic != null)
                {
                    // Hand over logic to the boss
                    epicBossLogic.StartBossBattle();
                }
                return; 
            }
        // Trigger player camera trip
        playerPathFollower.MoveTo(encounterIndex);
        if (encounterIndex < playerPathFollower.destinations.Length)
        {
            StartEncounter(playerPathFollower.destinations[encounterIndex].travelDuration + defaultEnemyAggroDelay); // Start next encounter after trip is done, with a little buffer
        }
        else
        {
            StartEncounter(defaultEnemyAggroDelay); // fallback, should not happen
        }
    }

    /// <summary>
    /// Starts encounter at encounter index and increments
    /// </summary>
    private void StartEncounter(float enemyAggroDelay)
    {
        if (encounterIndex > Encounters.Length - 1)
            return;
        
        int meleeCount = Encounters[encounterIndex].meleeEnemyCount;
        int projecCount = Encounters[encounterIndex].projectileEnemyCount;
        int minLegnth = Encounters[encounterIndex].minComboLength;
        int maxLength = Encounters[encounterIndex].maxComboLength;
        Encounters[encounterIndex].enemyManager.InitializeEncounter(meleeCount, projecCount, minLegnth, maxLength, enemyAggroDelay);
        
        encounterIndex++;
    }
}
