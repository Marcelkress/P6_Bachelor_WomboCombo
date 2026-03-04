using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
public class Enemy : MonoBehaviour
{
   
    private int[] comboArray = new int[] {  2, 3, 2, 1 ,3, 1 }; // a combo would be 1 and 2, then 3 and 1 would be another combo. 
    [SerializeField] private int comboStep = 0; // keeps track of the current step in the combo sequence
    private NavMeshAgent agent;

    public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)
    [SerializeField] private Transform content;
    private Image[] contentSprite;

    public Sprite SquareImage, CircleImage, TriangleImage; // references to the UI images for each button (Square, Circle, Triangle)
    public Transform player;
    private Player playerScript; // reference to the Player script to call the TakeDamage method

    private GridLayoutGroup gridLayoutGroup; // reference to the GridLayoutGroup component

    // Attack range and damage variables can be added here as needed
    public int damageAmount = 1; // Example damage amount
    public float attackCooldown = 1f; // Example cooldown time between attacks

    



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        player = GameObject.FindGameObjectWithTag("Player").transform; // Find the player by tag and get its transform
        playerScript = player.GetComponent<Player>(); // Get the Player script component from the player game object

        InitializeUI();
        // Set the destination to the player's position
        agent.SetDestination(player.position);
    }

    void Update()
    {
        DamagePlayer(); // Check if the player is within attack range and apply damage if necessary
    }

    public void PlayerOneUpdate()
    {
        
       
        // Epic Effects
        UpdateUI();
       
       
    }

    private void InitializeUI()
    {
        contentSprite = new Image[comboArray.Length]; // Initialize the contentSprite array to match the length of comboArray

        for (int i = 0; i < comboArray.Length; i++)
        {
            // Instantiate a new UI image for each combo step and set its parent to the content transform
            GameObject uiImage = Instantiate(inputUIImage, content);

            // 1 = Square, 2 = Circle, 3 = Triangle (you can customize this mapping as needed)

                switch (comboArray[i])
                {
                    case 1:
                        uiImage.GetComponent<Image>().sprite = SquareImage; // Set the sprite to the Square image
                        break;
                    case 2:
                        uiImage.GetComponent<Image>().sprite = CircleImage; // Set the sprite     to the Circle image
                        break;
                    case 3:
                        uiImage.GetComponent<Image>().sprite = TriangleImage; // Set the sprite to the Triangle image
                        break;

                }

            contentSprite[i] = uiImage.GetComponent<Image>(); // Store the Image component in the contentSprite array
        }

        
    }

    private void UpdateUI()
    {
       // animate the UI elements with shake effect and then disable the current combo step's UI elements
        int rightIndex = comboStep * 2 + 1;
        int leftIndex = comboStep * 2;

        // If we've already consumed all combo inputs, complete immediately.
        if (leftIndex >= comboArray.Length)
        {
            CheckComboCompletion();
            return;
        }

        if (leftIndex >= contentSprite.Length || rightIndex >= contentSprite.Length)
        {
            return;
        }

        Sequence comboStepSequence = DOTween.Sequence();

        comboStepSequence.Join(
            contentSprite[leftIndex].transform
                .DOShakePosition(0.5f, new Vector3(10f, 10f, 10f), 10, 90, false)
        );

        comboStepSequence.Join(
            contentSprite[rightIndex].transform
                .DOShakePosition(0.5f, new Vector3(10f, 10f, 10f), 10, 90, false)
        );

        comboStepSequence.OnComplete(() =>
        {
            contentSprite[leftIndex].enabled = false;
            contentSprite[rightIndex].enabled = false;

            comboStep++; // Move to the next step in the combo sequence
            CheckComboCompletion();
        });

        
    }

    private void CheckComboCompletion()
    {
        int totalSteps = comboArray.Length / 2;
        if (comboStep >= totalSteps)
        {
            Die();
        }
    }
    float currentattackCooldown = 0f; // Initialize the cooldown timer

    private void DamagePlayer()
    {
        if (Vector3.Distance(transform.position, player.position) < agent.stoppingDistance)
        {
             currentattackCooldown -= Time.deltaTime; // Decrease the cooldown timer by the time elapsed since the last frame
            if (currentattackCooldown <= 0f)
            {
            
            Debug.Log("Player Damaged!"); // Player is damaged
            playerScript.TakeDamage(damageAmount); // Call the TakeDamage method on the player script

            currentattackCooldown = attackCooldown; // Reset the cooldown timer
            }

        }
    }

    private void Die()
    {
        Debug.Log("Enemy Defeated!"); // Enemy is defeated
        // Add any additional logic for enemy defeat here (e.g., play animation, drop loot, etc.)
        Destroy(gameObject); // Destroy the enemy game object
    }
}
