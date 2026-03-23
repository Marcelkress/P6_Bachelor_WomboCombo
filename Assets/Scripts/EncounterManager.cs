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
        // Trigger player camera trip
        playerPathFollower.MoveTo(encounterIndex);
        StartEncounter(playerPathFollower.destinations[encounterIndex].travelDuration + defaultEnemyAggroDelay); // Start next encounter after trip is done, with a little buffer
        
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
