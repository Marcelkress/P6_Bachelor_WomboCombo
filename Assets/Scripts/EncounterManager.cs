using PathCreation.Examples;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct EncounterStruct
{
    public EnemyManager enemyManager;
    public int meleeEnemyCount, projectileEnemyCount;
    public int minComboLength, maxComboLength;
    public bool respawnPoint;
}
public class EncounterManager : MonoBehaviour
{
    public EncounterStruct[] Encounters;
    private int encounterIndex;
    public static EncounterManager instance;
    public PathFollower playerPathFollower;
    public float defaultEnemyAggroDelay = 2f;
    public EpicBossLogic epicBossLogic;

    public bool bossBattleStarted = false;

    [Header("Dev Tools")]
    [Tooltip("Set to skip ahead to a specific encounter (0 = normal start)")]
    public bool useDevTools = false;
    public int startFromEncounter = 0;

    public int lastRespawnPoint;

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

    public void RestartFromLastRespawn()
    {
        if (lastRespawnPoint < 0 || lastRespawnPoint >= playerPathFollower.destinations.Length)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        Encounters[encounterIndex-1].enemyManager.ClearAllEntities();

        //playerPathFollower.transform.position = playerPathFollower.destinations[lastRespawnPoint].transform.position;
        encounterIndex = lastRespawnPoint;
        Encounters[encounterIndex] = Encounters[encounterIndex]; // refresh encounter to reset enemy managers enemy lists and such
        GoToNextEncounter(true);
    }

    public void GoToNextEncounter(bool isRespawning = false)
    {
        if (bossBattleStarted) return;
            
            
        Debug.Log("All enemies dead, proceeding to next encounter"); 

            // Check if we have finished all regular encounters
            if (encounterIndex >= Encounters.Length)
            {
                Debug.Log("Starting Boss Encounter!");
                if (epicBossLogic != null)
                {
                    // Hand over logic to the boss
                    epicBossLogic.StartBossBattle();
                    bossBattleStarted = true;
                }
                
            }


        // Trigger player camera trip
        playerPathFollower.MoveTo(encounterIndex, isRespawning);
        if (isRespawning)
        {
            StartEncounter(0); // Start encounter immediately if respawning, since player is already at the correct position
            return;
        }
        if (Encounters[encounterIndex].respawnPoint)
        {
            lastRespawnPoint = encounterIndex; // what the last respawn point was, used for player death and retrying
        }
        if (encounterIndex < playerPathFollower.destinations.Length)
        {
            StartEncounter(playerPathFollower.GetCurrentTripDuration() + defaultEnemyAggroDelay); // Start next encounter after trip is done, with a little buffer
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
