using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3; // Example health value for the player
    private int currentHealth;  
    public UnityEvent PlayerDiedEvent;
    public int healAmount = 5;
    public int lives = 3;

    [Header("Health UI")]
    public Image healthBar; // Reference to the health bar UI element
    public TMP_Text healthText; // Reference to the health text UI element (optional)
    public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)
    [SerializeField] private Transform content;
    private Image[] contentSprite;
    public Sprite moonImg, starImg, sunImg; // references to the UI images for each button (Square, Circle, Triangle)
    public float shakeDuration;
    public Vector3 shakeStrength;
    public float shakeRandomness;
    public float healComboFadeTime = .3f;

    private int uiComboStep;
    
    [Header("Heal combo")]
    public int[] healingComboArray = new int[] {  2, 1, 2, 2, 2, 3 };

    public float starSuccessWindow = 0.5f;
    private int comboStep = 0; // keeps track of the current step in the combo sequence
    public bool debug = false;

    public int shakeVibrato;

    
    private PlayerInput playerInput; // Reference to the PlayerInput component for handling input actions
    private EncounterManager encounterManager; // Reference to the EncounterManager for accessing boss logic and other encounter-related data

    [Header("Fire ball")]
    public GameObject fireballPrefab;
    
    [Header("Boss")]
    public bool lookAtBoss = false; // whether the player should look at the boss, used for certain encounters and the boss fight

    public static bool healingComboStarted;

    private int deathCounterSaveInfo;
    
    void Start()
    {
        currentHealth = maxHealth; // Initialize current health to the maximum health at the start
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
            healthBar.fillAmount = (float)currentHealth / maxHealth; // Set the initial fill amount of the health bar
            healthText.text = currentHealth.ToString(); // Set the initial health text (optional)
        }
        
        contentSprite = new Image[healingComboArray.Length]; // Initialize the contentSprite array to match the length of healinghealingComboArray

        for (int i = 0; i < healingComboArray.Length; i++)
        {
            // Instantiate a new UI image for each combo step and set its parent to the content transform
            GameObject uiImage = Instantiate(inputUIImage, content);



                switch (healingComboArray[i])
                {
                    case 1:
                        uiImage.GetComponent<Image>().sprite = moonImg; 
                        break;
                    case 2:
                        uiImage.GetComponent<Image>().sprite = starImg; 
                        break;
                    case 3:
                        uiImage.GetComponent<Image>().sprite = sunImg; 
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
        CompareCombo(1);
    }
    public void PlayerTwoUpdate()
    {
        playerTwoInfo = InputManager.instance.GetPlayerSymbols(2);
        CompareCombo(2);
    }
    
    private PlayerInfoStruct playerOneInfo, playerTwoInfo;
    private bool pOneStar, pTwoStar, startTimer;
    private float timer;
    private void CompareCombo(int id)
    {
        Debug.Log("heal method");
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
            //Debug.Log(comboStep);

            if (healingComboArray[comboStep] == 2) // If the top symbol is square
            {
                if (playerOneInfo.symbOne == healingComboArray[comboStep] && id == 1)
                    pOneStar = true;

                if(playerTwoInfo.symbOne == healingComboArray[comboStep] && id == 2)
                    pTwoStar = true;

                startTimer = true;

                Debug.Log("returning");

                return;
            }

            healingComboStarted = true;
            Enemy.globalComboStarted = true;

            // Completed one combo step
            UpdateUI();

            comboStep += 2;
            
            if (comboStep >= healingComboArray.Length)
            {
                // Completed entire combo
                healingComboStarted = false;
                Enemy.globalComboStarted = false;
            }
        }
    }

    void Update()
    {
        if (startTimer)
        {
            timer += Time.deltaTime;
            
            if (pOneStar == true && pTwoStar == true) // both players pressed within the window
            {
                //Debug.Log("Both players pressed star top");
                if (timer < starSuccessWindow) // only succeed if still within the time window
                {
                    comboStep += 2;
                    UpdateUI();
                }
                else
                {
                    //Debug.Log("Too slow - resetting");
                }
                
                timer = 0;
                startTimer = false;
                pOneStar = false;
                pTwoStar = false;
            }
            else if (timer >= starSuccessWindow)
            {
                //Debug.Log("Resetting after time");
                timer = 0;
                startTimer = false;
                pOneStar = false;
                pTwoStar = false;
            }
        }
    }
    
    public void UpdateUI()
    {
        // animate the UI elements with shake effect and then disable the current combo step's UI elements
        int top = uiComboStep;
        int bottom = uiComboStep + 1;

        uiComboStep += 2;

        // If we've already consumed all combo inputs, complete immediately.
        if (bottom >= healingComboArray.Length)
        {
            return;
        }

        if (bottom >= contentSprite.Length || top >= contentSprite.Length)
        {
            return;
        }
        
        Sequence comboStepSequence = DOTween.Sequence();
        comboStepSequence.Join(
            contentSprite[bottom].transform
                .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false)
        );
        
        comboStepSequence.Join(
            contentSprite[top].transform
                .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false)
        );

        comboStepSequence.OnComplete(() =>
        {
            contentSprite[bottom].DOFade(0, healComboFadeTime);
            contentSprite[top].DOFade(0, healComboFadeTime);

            //comboStep++; // Move to the next step in the combo sequence
            CheckHealCompletion();
            comboStepSequence.Kill();
        });
    }

    private void CheckHealCompletion()
    {
        Debug.Log("Checking for full health");
        if (uiComboStep >= healingComboArray.Length)
        {
            foreach (var img in contentSprite)
            {
                img.DOFade(1, healComboFadeTime);
                uiComboStep = 0;
            }

            currentHealth += 5;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            
            AnimateHealthBar();
            healthText.text = currentHealth.ToString();
        }
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage; // Reduce the player's health by the damage amount
        healthText.text = currentHealth.ToString(); // Update the health text (optional)
        //Debug.Log("Player took damage! Current health: " + currentHealth);

        // Update the health bar UI element
        if (healthBar != null)
        {

            if (currentHealth < maxHealth/2)
            {
                healthBar.fillAmount = (float)currentHealth / maxHealth;
                
                 // Optional: Add a tweening effect to the health bar for smoother transitions
                AnimateHealthBar();
                ShakeHealthBar();

            }
            else
            {
                healthBar.fillAmount = (float)currentHealth / maxHealth;
                AnimateHealthBar();
            }
        }

        if (currentHealth == 0)
        {
            if (dead) return;
            dead = true;
            Die(); // Call the Die method if health drops to 0
        }

    }

    private bool dead;

    private void AnimateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.DOFillAmount((float)currentHealth / maxHealth, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    private void ShakeHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.transform.DOShakePosition(0.5f, new Vector3(10f, 0f, 0f), 10, 90, false);
        }
    }

    private void Die()
    {
        lives--;
        deathCounterSaveInfo++;
        PlayerDiedEvent.Invoke();
        currentHealth = maxHealth;
        dead = false;
        //gameOverScreen.SetActive(true); // Show the Game Over screen
        //Time.timeScale = 0f; // Pause the game by setting time scale to
    }

    private void OnDisable()
    {
        SaveSystem.SaveData(Enemy.wrongComboInput, "Wrong combo Inputs");
        //SaveSystem.SaveData(deathCounterSaveInfo, "deathCounter");
    }
}
