using UnityEngine;
using PathCreation.Examples;
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
    [SerializeField] private BossComboSystem bossComboSystem;
    [SerializeField] private PathFollower bossPathFollower;

    [Header("Player Destinations")]
    public PathDestinationObject[] destinations;
    [Header("Boss settings")]
    public float decisionInterval = 3f; // how often the boss decides on a new action in phase 2, can be used for a specific attack pattern in phase 2 or 3 where the boss moves around a lot
    public float moveSpeed;

    [Header("Boss movement destinations")]
    public PathDestinationObject[] bossRetreatDestinations; // when moving other places
    public PathDestinationObject[] bossFlyDestinations; // when moving to attack the player, maybe can be used for a specific attack pattern in phase 2 or 3 where the boss moves around a lot
    public Transform player; // for moving to the player when having to attack
    
    private Player playerScript;


    [Header("Spawning")]
    public EnemyManager bossMinionManager;

    public BossPhase currentPhase = BossPhase.Idle;


    public bool bossGothitted = false; // placeholder
    public bool minionsCleared = false;
    public NavMeshAgent bossNavMeshAgent;

    private float decisionTimer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        bossNavMeshAgent = GetComponent<NavMeshAgent>();
        bossNavMeshAgent.speed = moveSpeed;

        encounterManager = EncounterManager.instance;
        if (encounterManager != null)
        {
            playerPathFollower = encounterManager.playerPathFollower;
        }

        if (player == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                playerScript = playerGo.GetComponent<Player>();
                player = playerGo.transform;
            }
        }
        
    }

    private void Update()
    {
        if (currentPhase == BossPhase.Phase1)
        {
            // Check for player input to trigger boss hit
            if (bossGothitted)
            {
                bossPathFollower.MoveTo(bossRetreatDestinations[Random.Range(0, bossRetreatDestinations.Length)]); // move to random retreat destination when hit, can be used for a specific attack pattern in phase 2 or 3 where the boss moves around a lot

                playerPathFollower.MoveTo(destinations[1]);
                bossGothitted = false; // reset hit state

                // reset bossagent destination so it does not move
                bossNavMeshAgent.isStopped = true;

                if (bossComboSystem.bossComboArray.Length <= 0)
                {
                    // phase 2
                    bossComboSystem.RandomArray(4, 8);
                    StartPhaseTwo();
                }
                Invoke(nameof(StartPhaseOne), 2f); 
            }
        }
        else if (currentPhase == BossPhase.Phase2)
        {
            if (bossComboSystem.bossComboArray.Length <= 0)
            {
                // phase 2
                bossComboSystem.RandomArray(4, 8);
                StartPhaseThree();
            }
            decisionTimer += Time.deltaTime;
            if (decisionTimer >= decisionInterval)
            {
                bossPathFollower.MoveTo(bossFlyDestinations[Random.Range(0, bossFlyDestinations.Length)]);
                playerScript.lookAtBoss = true;
                Invoke(nameof(ShootFireBall), 1.5f); // move player to random node every few seconds, can be used for a specific attack pattern in phase 2 or 3 where the boss moves around a lot
                decisionTimer = 0f;
            }
        }
        else if (currentPhase == BossPhase.Phase3)
        {
            Time.timeScale = 0.5f; // slow down time for dramatic effect in final phase
            

        }
    }

    private void ShootFireBall()
    {
        // shooting fireball towards player.
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
        bossPathFollower.MoveTo(0); // move to initial position, can be the same as one of the bossMovementDestinations, but ensures it is on the path. Maybe can be used for a dramatic entrance.
        currentPhase = BossPhase.Phase1;
        Debug.Log("Phase 1, spawn minions."); // maybe boss cannot be hit while minions are alive?
        bossMinionManager.InitializeEncounter(2, 0, 4, 2, 1f); // spawn some minions with random combos, but ensure they have unique starting combos for the first two steps
        minionsCleared = false;

        bossNavMeshAgent.SetDestination(player.position); // move towards player, Cant be hit until minions are cleared.
    }

    public void OnMinionsCleared()
    {
        if (currentPhase == BossPhase.Phase1)
        {
            minionsCleared = true;
            //localComboStarted = false;
            //globalComboStarted = false;
            Debug.Log("Minnions cleared. Can attack boss");
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
