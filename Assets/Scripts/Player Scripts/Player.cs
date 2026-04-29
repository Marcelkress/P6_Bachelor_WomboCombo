using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3; // Example health value for the player
    [HideInInspector] public int currentHealth;  
    public UnityEvent PlayerDiedEvent, PlayerRespawnEvent;
    public int healAmount = 5;
    public int lives = 3;
    public UnityEvent livesChangedEvent;

    public Image takeDamageEffect; // Reference to a UI Image that will flash when the player takes damage
    public float damageFlashDuration = 0.2f; // Duration of the damage flash effect
    public float damageFlashIntensity = 0.5f; // Intensity of the damage flash effect (0 to 1)
    

    [Header("Epicness buildup")]
    public float epicnessIncreasePerHit = 0.1f;
    public float epicnessDecreasePerMiss = 0.05f;
    public float maxEpicness = 1f;
    public float epicnessThresholdForSpell = 0.8f; // The epicness level required to be able to shoot a magic spell
    private float startingEpicness = 0f;
    public float currentEpicness = 0f;
    public event System.Action<float> EpicnessChanged;

    [Header("Character Wobbing effect")]
    public float amplitude = 0.1f; // The maximum distance the player will wobble
    public float frequency = 1f; // The speed of the wobble
    public Transform wobbleTransform; // The transform that will be wobbled (e.g., the player's body or a child object)

    [Header("Health UI")]
    public RectTransform healthBarUI; // Reference to the health bar UI element
    public Material healthBarMaterial; // Reference to the material used for the health bar
    public TMP_Text healthText; // Reference to the health text UI element (optional)
    public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)
    [SerializeField] private Transform content;
    private Image contentImage;
    private Color contentOriginalColor;
    private float contentImageOriginalAlpha;
    public float contentFadeDuration = 0.5f;



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


    [Header("Magic spell")]
    public GameObject[] MagicSpellPrefabs; // Array of different magic spell prefabs that the player can shoot
    public Transform[] shootingPoints;
    [SerializeField] private MagicBall magicBall;

    private PlayerInput playerInput; // Reference to the PlayerInput component for handling input actions
    private EncounterManager encounterManager; // Reference to the EncounterManager for accessing boss logic and other encounter-related data

    //public UnityEvent FireballSound;
    
    [Header("Boss")]
    public bool lookAtBoss = false; // whether the player should look at the boss, used for certain encounters and the boss fight

    public static bool healingComboStarted;

    private int deathCounterSaveInfo;
    
    void Start()
    {
        contentImage = content.GetComponent<Image>();
        contentOriginalColor = contentImage.color;
        contentImageOriginalAlpha = contentImage.color.a;

        takeDamageEffect.DOFade(0f, 0f); // Ensure the damage effect is invisible at the start
        currentHealth = maxHealth; // Initialize current health to the maximum health at the start
        playerInput = GetComponent<PlayerInput>(); // Get the PlayerInput component attached to the player game object
        encounterManager = EncounterManager.instance; // Get the instance of the EncounterManager
        InitializeUI();

        if (InputManager.instance != null)
        {
            InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
            InputManager.instance.PlayerTwoEvent.AddListener(PlayerTwoUpdate);
        }

        StartCoroutine(StartWobbleEffect());
    }

    private IEnumerator StartWobbleEffect()
    {
        Vector3 originalPosition = wobbleTransform.localPosition;
        float elapsedTime = 0f;

        while (true)
        {
            elapsedTime += Time.deltaTime;
            float wobbleX = Mathf.Sin(elapsedTime * frequency) * amplitude;
            float wobbleY = Mathf.Cos(elapsedTime * frequency) * amplitude;
            wobbleTransform.localPosition = originalPosition + new Vector3(wobbleX, wobbleY, 0f);
            yield return null; // Wait for the next frame
        }
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
        if (healthBarMaterial != null)
        {
            healthBarMaterial.SetFloat("_FillLevel", maxHealth); // Set the initial fill level of the health bar material
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


    [SerializeField] private float lightningCleanupDelay = 1f;
    private List<GameObject> instantiatedMagicSpells = new List<GameObject>();

    public void AddEpicness(float delta)
    {
        float nextEpicness = Mathf.Clamp(currentEpicness + delta, 0f, maxEpicness);

        if (Mathf.Approximately(nextEpicness, currentEpicness))
            return;

        currentEpicness = nextEpicness;
        EpicnessChanged?.Invoke(currentEpicness);
    }

    public void ShootMagicspell(Transform target, GameObject targetedEnemy, bool isProjectile = false)
    {
        if (magicBall != null)
            magicBall.PlayCastFeedback();

        int spellIndex = Random.Range(0, MagicSpellPrefabs.Length);
        int shootingPointIndex = Random.Range(0, shootingPoints.Length);
        
        Vector3 targetPos = target.position;

        if (currentEpicness >= epicnessThresholdForSpell && !isProjectile) // kan ikke epic boom på projectiles
        {
            
            currentEpicness = 0f; // Reset epicness after shooting the powerful spell
            EpicnessChanged?.Invoke(currentEpicness); // Notify listeners of the epicness change
            magicBall.StartCoroutine(magicBall.SpellAttack(targetedEnemy, true));
            magicBall.BombTimerStart();
            return; // Exit the method to prevent shooting a regular spell
        }

       // if (spellIndex == 0) spellIndex = 1; // TEMPORARY 
        
        if (spellIndex == 0)
        {
            GameObject fireball = Instantiate(MagicSpellPrefabs[spellIndex], shootingPoints[shootingPointIndex].position, shootingPoints[shootingPointIndex].rotation);

            FireballProjectile fireballScript = fireball.GetComponent<FireballProjectile>();

            fireballScript.SetTargetTransform(target, targetedEnemy);
        
            // FireballSound.Invoke();
        }
        else if (spellIndex == 1)
        {
            GameObject lightning = Instantiate(MagicSpellPrefabs[spellIndex], shootingPoints[shootingPointIndex].position, shootingPoints[shootingPointIndex].rotation);
            instantiatedMagicSpells.Add(lightning);

            Vector3 direction = targetPos - lightning.transform.position;
            
            if (direction.sqrMagnitude > 0.0001f)
            {
                lightning.transform.rotation = Quaternion.LookRotation(direction.normalized);
            }

            RFX4_RaycastCollision raycastCollision = lightning.GetComponentInChildren<RFX4_RaycastCollision>();
            IEnemyDamagable enemyDamagable = targetedEnemy.GetComponent<IEnemyDamagable>();

            if (raycastCollision != null)
            {
                bool hasAppliedHit = false;
                raycastCollision.CollisionEnter += (sender, collisionInfo) =>
                {
                    if (hasAppliedHit || enemyDamagable == null) return;
                    hasAppliedHit = true;
                    enemyDamagable.UpdateUI();
                };

                raycastCollision.UpdateRaycast(targetedEnemy);
            }

            StartCoroutine(waitAndCleanUp(lightning, lightningCleanupDelay));
        }
    }

    private IEnumerator waitAndCleanUp(GameObject spell, float delay)
    {
        yield return new WaitForSeconds(delay);
        CleanUpSpell(spell);
    }

    private void CleanUpSpell(GameObject spell)
    {
        if (spell != null)
        {
            Destroy(spell);
        }

        instantiatedMagicSpells.Remove(spell);
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
                succesfullComboDone = false;

                Debug.Log("returning");

                return;
            }

            /*
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
            }*/

        }
    }

    private bool succesfullComboDone = false;

    void Update()
    {
        if (startTimer)
        {
            timer += Time.deltaTime;
            
            if (pOneStar == true && pTwoStar == true) // both players pressed within the window
            {
                //Debug.Log("Both players pressed star top");
                if (timer < starSuccessWindow && !succesfullComboDone) // only succeed if still within the time window
                {
                    succesfullComboDone = true;
                    comboStep += 2;
                    healingComboStarted = true;
                    Enemy.globalComboStarted = true;

                    // Completed one combo step
                    UpdateUI();
                    if (comboStep >= healingComboArray.Length)
                    {
                        // Completed entire combo
                        healingComboStarted = false;
                        Enemy.globalComboStarted = false;
                        comboStep = 0;
                    }       
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

        contentImage.DOFade(1, healComboFadeTime);
        contentImage.color = Color.white;

        
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

            currentHealth += healAmount;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            contentImage.DOFade(contentImageOriginalAlpha, healComboFadeTime);
            contentImage.color = contentOriginalColor;

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
        if (healthBarMaterial != null)
        {

           
            healthBarMaterial.SetFloat("_FillLevel", (float)currentHealth / maxHealth);
            FlashDamageEffect();
            // TODO: Add a tweening effect to the health bar for smoother transitions
            AnimateHealthBar();
            ShakeHealthBar();
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
        if (healthBarMaterial != null)
        {
            healthBarMaterial.SetFloat("_FillLevel", (float)currentHealth / maxHealth);
        }
    }

    private void ShakeHealthBar()
    {
        if (healthBarUI == null) return;

        healthBarUI.DOShakeAnchorPos(0.5f, new Vector2(10f, 0f), 10, 90, false, true);
    }

    private void FlashDamageEffect()
    {
        if (takeDamageEffect == null) return;

        takeDamageEffect.DOKill();

        takeDamageEffect.DOFade(damageFlashIntensity, damageFlashDuration).OnComplete(() =>
        {
            takeDamageEffect.DOFade(0f, damageFlashDuration);
        });
    }

    private void Die()
    {
        lives--;
        livesChangedEvent.Invoke();
        deathCounterSaveInfo++;
        if (lives <= 0) // real dead FR
        {
            PlayerDiedEvent.Invoke();
            return;
        }
        PlayerRespawnEvent.Invoke(); // fake dead FR
        currentHealth = maxHealth;
        dead = false;
    }

    private void OnDisable()
    {
        SaveSystem.SaveData(Enemy.wrongComboInput, "Wrong combo Inputs");
        //SaveSystem.SaveData(deathCounterSaveInfo, "deathCounter");
    }

    public void ResetHealingCombo()
    {
        comboStep = 0;
        uiComboStep = 0;
        healingComboStarted = false;
        Enemy.globalComboStarted = false;

        foreach (var img in contentSprite)
        {
            img.DOFade(1, healComboFadeTime);
        }

        contentImage.DOFade(contentImageOriginalAlpha, healComboFadeTime);
        contentImage.color = contentOriginalColor;
    }
}
