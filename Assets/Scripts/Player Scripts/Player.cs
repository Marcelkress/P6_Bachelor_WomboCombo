using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int health = 3; // Example health value for the player
    private int currentHealth;  
    public UnityEvent PlayerDiedEvent;

    [Header("Health UI")]
    public Image healthBar; // Reference to the health bar UI element
    public TMP_Text healthText; // Reference to the health text UI element (optional)
    public Sprite SquareImage, CircleImage, TriangleImage;
    public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)
    [SerializeField] private Transform content;
    private Image[] contentSprite;
    
    [Header("Heal combo")]
    public int[] healingComboArray = new int[] {  2, 1, 2, 2, 2, 3 };
    private int comboStep = 0; // keeps track of the current step in the combo sequence

    private PlayerInput playerInput; // Reference to the PlayerInput component for handling input actions
    private EncounterManager encounterManager; // Reference to the EncounterManager for accessing boss logic and other encounter-related data

    [Header("Fire ball")]
    public GameObject fireballPrefab;
    
    [Header("Boss")]
    public bool lookAtBoss = false; // whether the player should look at the boss, used for certain encounters and the boss fight

    public static bool healingComboStarted;
    
    void Start()
    {
        currentHealth = health; // Initialize current health to the maximum health at the start
        playerInput = GetComponent<PlayerInput>(); // Get the PlayerInput component attached to the player game object
        encounterManager = EncounterManager.instance; // Get the instance of the EncounterManager
        InitializeUI();
        InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
        InputManager.instance.PlayerTwoEvent.AddListener(PlayerTwoUpdate);
    }
    private void LateUpdate()
    {
        if (lookAtBoss)
        {
            Vector3 dir = encounterManager.epicBossLogic.transform.position - transform.position;
            dir.y = 0f; // Keep the y-axis rotation unchanged
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
    private void InitializeUI()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)currentHealth / health; // Set the initial fill amount of the health bar
            healthText.text = currentHealth.ToString(); // Set the initial health text (optional)
        }
        
        contentSprite = new Image[healingComboArray.Length]; // Initialize the contentSprite array to match the length of healinghealingComboArray

        for (int i = 0; i < healingComboArray.Length; i++)
        {
            // Instantiate a new UI image for each combo step and set its parent to the content transform
            GameObject uiImage = Instantiate(inputUIImage, content);

            // 1 = Square, 2 = Circle, 3 = Triangle (you can customize this mapping as needed)

                switch (healingComboArray[i])
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

    public void ShootFireball(Transform target, GameObject targetedEnemy)
    {
        GameObject fireball = Instantiate(fireballPrefab, this.transform.position, this.transform.rotation);

        FireballProjectile fireballScript = fireball.GetComponent<FireballProjectile>();

        fireballScript.SetTargetTransform(target, targetedEnemy);

    }
    
    public void PlayerOneUpdate()
    {
        playerOneInfo = InputManager.instance.GetPlayerSymbols(1);
        //CompareCombo(1);
    }
    public void PlayerTwoUpdate()
    {
        playerTwoInfo = InputManager.instance.GetPlayerSymbols(2);
        //CompareCombo(2);
    }
    
    private PlayerInfoStruct playerOneInfo, playerTwoInfo;
    private int rightIndex = 0;
    private int leftIndex = 0;
   /* private void CompareCombo(int id)
    {
        if (healingComboStarted == false)
        {
            if (Enemy.globalComboStarted)
            {
                return;
            }
        }

        if (debug)
        {
            Debug.Log("ComboStep: " + comboStep);
            Debug.Log("Array symb one: " + healingComboArray[comboStep]);
            Debug.Log("Array symb two: " + healingComboArray[comboStep + 1]);
        }

        // If either player one or player two symbols are correct continue
        if ((playerOneInfo.symbOne == healingComboArray[comboStep] && playerOneInfo.symbTwo == healingComboArray[comboStep + 1])
            || (playerTwoInfo.symbOne == healingComboArray[comboStep] && playerTwoInfo.symbTwo == healingComboArray[comboStep + 1]))
        {
            Debug.Log(comboStep);

            if (healingComboArray[comboStep] == 2) // If the top symbol is square
            {
                if (playerOneInfo.symbOne == healingComboArray[comboStep] && id == 1)
                    pOneSquare = true;

                if(playerTwoInfo.symbOne == healingComboArray[comboStep] && id == 2)
                    pTwoSquare = true;

                startTimer = true;

                Debug.Log("returning");

                return;
            }

            Debug.Log("Shooting from Method");

            healingComboStarted = true;
            Enemy.globalComboStarted = true;

            // Completed one combo step
            //UpdateUI(); Vi opdatere istedet når fireball rammer enemy

            comboStep += 2;
            
            if (comboStep >= healingComboArray.Length)
            {
                // Completed entire combo
                localComboStarted = false;
                globalComboStarted = false;
            }
        }
    }
    */
    public void TakeDamage(int damage)
    {
        currentHealth -= damage; // Reduce the player's health by the damage amount
        healthText.text = currentHealth.ToString(); // Update the health text (optional)
        Debug.Log("Player took damage! Current health: " + currentHealth);

        // Update the health bar UI element
        if (healthBar != null)
        {

            if (currentHealth < health/2)
            {
                healthBar.fillAmount = (float)currentHealth / health;
                
                 // Optional: Add a tweening effect to the health bar for smoother transitions
                AnimateHealthBar();
                ShakeHealthBar();

            }
            else
            {
                healthBar.fillAmount = (float)currentHealth / health;
                AnimateHealthBar();
            }
        }

        if (currentHealth <= 0)
        {
            Die(); // Call the Die method if health drops to 0 or below
        }

    }

    private void AnimateHealthBar()
    {
        // Example of using DOTween to animate the health bar fill amount
        if (healthBar != null)
        {
            healthBar.DOFillAmount((float)currentHealth / health, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    private void ShakeHealthBar()
    {
        if (healthBar != null)
        {
            // Example of using DOTween to shake the health bar
            healthBar.transform.DOShakePosition(0.5f, new Vector3(10f, 0f, 0f), 10, 90, false);
        }
    }

    private void Die()
    {
        PlayerDiedEvent.Invoke();
        //gameOverScreen.SetActive(true); // Show the Game Over screen
        //Time.timeScale = 0f; // Pause the game by setting time scale to
    }

}
