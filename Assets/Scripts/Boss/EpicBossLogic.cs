using System.Collections;
using UnityEngine;
using PathCreation.Examples;
using UnityEngine.AI;

public class EpicBossLogic : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PathFollower playerPathFollower;
    [SerializeField] private PathFollower bossPathFollower;

    [Header("Player Destinations")]
    public PathDestinationObject[] destinations;

    [Header("Boss Movement Destinations")]
    public PathDestinationObject[] bossRetreatDestinations;
    public PathDestinationObject[] bossFlyDestinations;

    [Header("Testing Settings")]
    [Tooltip("How often to pick a new destination")]
    public float movementInterval = 4f;
    public bool autoTestMovement = true;
    public bool movePlayer = true;
    public bool moveBoss = true;

    private int lastPlayerIndex = -1;
    private int lastBossFlyIndex = -1;
    private int lastBossRetreatIndex = -1;

    public BossPhase currentPhase = BossPhase.Phase1; // Just to avoid breaking references
    public bool bossGothitted = false; 
    public bool minionsCleared = true; 

    private bool shouldLookAtBoss = false;

    private Vector3 originalCameraRotation;

    public enum BossPhase { Idle, Phase1, Phase2, Phase3 }

    private void Start()
    {
        if (autoTestMovement)
        {
            StartCoroutine(TestMovementRoutine());
        }
    }

    private void Update()
    {        // Simple hit reaction test
        if (bossGothitted)
        {
            bossGothitted = false;
            if (bossRetreatDestinations.Length > 0 && bossPathFollower != null)
            {
                int index = Random.Range(0, bossRetreatDestinations.Length);
                bossPathFollower.MoveTo(bossRetreatDestinations[index]);
                Debug.Log($"Boss hit! Retreating to {bossRetreatDestinations[index].name}");
            }
        }

        if (shouldLookAtBoss)
        {
            originalCameraRotation = GetCurrentCameraRotation();
            PlayerCameraLookAtBoss();
        }
        else
        {
            // Smoothly return camera to original rotation
            Quaternion targetRotation = Quaternion.Euler(originalCameraRotation);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, targetRotation, Time.deltaTime * 2f);
        }
    }

    

    private IEnumerator TestMovementRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(movementInterval);

            if (movePlayer && destinations.Length > 0 && playerPathFollower != null)
            {
                int pIndex = GetRandomIndex(destinations.Length, lastPlayerIndex);
                lastPlayerIndex = pIndex;
                playerPathFollower.MoveTo(destinations[pIndex]);
                Debug.Log($"Player moving to {destinations[pIndex].name}");
            }

            if (moveBoss && bossPathFollower != null)
            {
                bool useFly = Random.value > 0.3f;
                var targetArray = useFly ? bossFlyDestinations : bossRetreatDestinations;
                
                if (targetArray != null && targetArray.Length > 0)
                {
                    int previousIndex = useFly ? lastBossFlyIndex : lastBossRetreatIndex;
                    int bIndex = GetRandomIndex(targetArray.Length, previousIndex);
                    
                    if (useFly) lastBossFlyIndex = bIndex;
                    else lastBossRetreatIndex = bIndex;

                    bossPathFollower.MoveTo(targetArray[bIndex]);
                    string type = useFly ? "Fly" : "Retreat";
                    Debug.Log($"Boss moving to {type} destination: {targetArray[bIndex].name}");
                }
            }
        }
    }

    private int GetRandomIndex(int arrayLength, int previousIndex)
    {
        if (arrayLength <= 1) return 0;
        int randomIndex = Random.Range(0, arrayLength);
        if (randomIndex == previousIndex)
        {
            randomIndex = (randomIndex + 1) % arrayLength;
        }
        return randomIndex;
    }

    private void PlayerCameraLookAtBoss()
    {
        Camera.main.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }

    public void StartBossBattle() { Debug.Log("StartBossBattle called"); }
    public void OnMinionsCleared() { minionsCleared = true; }
    public void StartPhaseTwo() { currentPhase = BossPhase.Phase2; }
    public void StartPhaseThree() { currentPhase = BossPhase.Phase3; }
    public void MovePlayerToRandomNode() 
    {
        if (destinations.Length > 0 && playerPathFollower != null)
        {
            int pIndex = GetRandomIndex(destinations.Length, lastPlayerIndex);
            lastPlayerIndex = pIndex;
            playerPathFollower.MoveTo(destinations[pIndex]);
        }
    }


    private Vector3 GetCurrentCameraRotation()
    {
        return Camera.main.transform.rotation.eulerAngles;
    }
}
