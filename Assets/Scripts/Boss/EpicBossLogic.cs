using UnityEngine;
using PathCreation.Examples;
using DG.Tweening;
using UnityEngine.AI;   

public enum BossPhase
{
    Idle,
    Phase1,
    Phase2,
    Phase3
}

public class EpicBossLogic : MonoBehaviour
{

    [SerializeField] private EncounterManager encounterManager;
    [SerializeField] private PathFollower playerPathFollower;

    [Header("Player Destinations")]
    public PathDestinationObject[] destinations;

    [Header("Boss settings")]
    public int[] bossComboArrayPhase1; // health
    public int[] bossComboArrayPhase2; // health
    public int[] bossComboArrayPhase3; // health
    public float moveSpeed;

    [Header("Boss movement destinations")]
    public Transform[] bossMovementDestinations; // when moving other places
    public Transform player; // for moving to the player when having to attack


    [Header("Spawning")]
    public EnemyManager bossMinionManager;

    public BossPhase currentPhase = BossPhase.Idle;



    private NavMeshAgent bossNavMeshAgent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bossNavMeshAgent = GetComponent<NavMeshAgent>();
        bossNavMeshAgent.speed = moveSpeed;
        encounterManager = EncounterManager.instance;
        if (encounterManager != null)
        {
            playerPathFollower = encounterManager.playerPathFollower;
        }
    }

    /// Called by EncounterManager when all regular encounters are done.
    public void StartBossBattle()
    {
        Debug.Log("Epic Boss Battle Started!");
        if (destinations.Length > 0)
        {
            playerPathFollower.MoveTo(destinations[0]);
            Invoke(nameof(StartPhaseOne), destinations[0].travelDuration + 1f); // brief delay upon arrival
        }
        else
        {
            StartPhaseOne();
        }
    }

    private void StartPhaseOne()
    {
        currentPhase = BossPhase.Phase1;
        Debug.Log("Phase 1, spawn minions."); // maybe boss cannot be hit while minions are alive?
        // e.g. bossMinionManager.InitializeEncounter(...);
    }

    public void OnMinionsCleared()
    {
        if (currentPhase == BossPhase.Phase1)
        {
            Debug.Log("Minnions cleared. Can attack boss");
            // Reveal combo UI
        }
    }

    public void StartPhaseTwo()
    {
        currentPhase = BossPhase.Phase2;
        Debug.Log("Phase 2: Boss flees, player camera follows dynamically");
        // Boss flees, player camera follows dynamically
        // MovePlayerToRandomNode();
    }

    public void StartPhaseThree()
    {
        currentPhase = BossPhase.Phase3;
        Debug.Log("Phase 3: The Wombo Combo, boss make epic final attach pattern with time slow");
        // Time slows, final attack pattern
    }

    public void MovePlayerToRandomNode()
    {
        if (destinations.Length > 0)
        {
            int randomIndex = Random.Range(0, destinations.Length);
            playerPathFollower.MoveTo(destinations[randomIndex]);
        }
    }

}
