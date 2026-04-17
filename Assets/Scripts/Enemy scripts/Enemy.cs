using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using DG.Tweening;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour, IEnemyDamagable
{
    [Header("Enemy type")] 
    public bool projectileEnemy;
    public GameObject projectilePrefab;
    public Transform projectileShootPoint;
    public float shootIntervalMax, shootIntervalMin;

    [Header("Movement and Aggro for non-projectile enemies")]
    public bool shouldCharge;
    public float chargeSpeedMultiplier = 2f; // Multiplier for the NavMeshAgent speed when charging

    [Header("Size Scaling")]
    public float baseScale = 1f;
    public float sizePerComboPair = 0.08f;
    public float maxScale = 1.8f;
    public float randomScaleVariance = 0.1f; // Random variance to add to the scale for visual diversity

    [Header("Combo")]
    public int[] comboArray;
    [SerializeField] private int comboStep = 0;
    private int uiComboStep;
    [HideInInspector] public int comboLength;
    private NavMeshAgent agent;
    private float currentattackCooldown = 0f; // Initialize the cooldown timer
    private Image[] contentSprite;
    private Transform player;
    private Vector3 targetPoint; // variable to store the closest point on the player's collider
    private BoxCollider playerCollider;
    private Player playerScript; // reference to the Player script to call the TakeDamage method
    private GridLayoutGroup gridLayoutGroup; // reference to the GridLayoutGroup component
    public Material enemyMaterial; // Reference to the enemy's material for visual feedback (e.g., flashing when hit)  
    public MeshRenderer enemyMeshRenderer;

    [Header("Visual Feedback")]
    private Light enemySpotLight; // for visuelt feedback når man targeter enemy
    public float lightFeedbackIntensity = 4f; 
    public float visualFeedbackFadeDuration = 0.1f;

    public Color uiBorderColor;

    public bool debug = false;
    // ComboCheck
    public static bool globalComboStarted;
    private bool localComboStarted;

    [Header("Attack")]
    public int damageAmount = 1; 
    public float attackCooldown = 1f;
    public float stoppingDistance = 1f;
    
    [Header("UI")] 
    [SerializeField] private Transform content;

    public GameObject canvas;

    public Sprite moonImg, starImg, sunImg; // references to the UI images for each button (Square, Circle, Triangle)
    public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)
    public float shakeDuration;
    public Vector3 shakeStrength;
    public float shakeRandomness;
    public int shakeVibrato;

    private bool isDead = false;
    public static int wrongComboInput;

    [Header("Animation")] public Animator anim;
    
    
    private PlayerInfoStruct playerOneInfo, playerTwoInfo;

    public EnemyManager manager;

    private float speed;

    private Canvas canvasComponent;
    public Image canvasImage;
    private float lastCanvasImageAlpha;
    private Color lastCanvasImageColor;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemySpotLight = GetComponentInChildren<Light>();
        enemySpotLight.intensity = 0; // Start with the spotlight off

        canvasComponent = canvas.GetComponent<Canvas>();
        lastCanvasImageAlpha = canvasImage.color.a;
        lastCanvasImageColor = canvasImage.color;

        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        comboLength = comboArray.Length / 2;
        UpdateSizeFromComboLength();
        if (shouldCharge)
        {
            agent.speed = agent.speed * chargeSpeedMultiplier;

            speed = agent.speed; // Store the original speed value for later use when resetting speed after hit recovery
            
        }
        else
        {
            speed = agent.speed;
        }
        
        InitializeUI(); 
        if (InputManager.instance != null)
        {
            InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
            InputManager.instance.PlayerTwoEvent.AddListener(PlayerTwoUpdate);
        }
        SetRandomColor();

    }

    private void OnDisable()
    {
        // Ensure this enemy does not leave the global combo lock set when disabled.
        if (localComboStarted)
        {
            localComboStarted = false;
            globalComboStarted = false;
        }

        startTimer = false;
        pOneSquare = false;
        pTwoSquare = false;
        timer = 0f;

        if (InputManager.instance != null)
        {
            InputManager.instance.PlayerOneEvent.RemoveListener(PlayerOneUpdate);
            InputManager.instance.PlayerTwoEvent.RemoveListener(PlayerTwoUpdate);
        }
    }

    void SetRandomColor()
    {
        // Clone the material so each enemy has its own instance
        enemyMaterial = new Material(enemyMaterial);
        enemyMeshRenderer.material = enemyMaterial;

        Color randomColor = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);

        enemyMaterial.SetColor("_LitColor", randomColor);
        Color color = enemyMaterial.GetColor("_LitColor");
        enemyMaterial.SetColor("_ShadowColor", color * 0.5f);
        uiBorderColor = randomColor;

    }

    public void Initialize(float aggroDelay)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform; // Find the player by tag
        playerScript = player.GetComponent<Player>();        

        Invoke(nameof(AggroPlayer), aggroDelay);
    
    }


    private void UpdateSizeFromComboLength()
    {
        float newScale = baseScale + (comboLength - 1) * sizePerComboPair; // Calculate new scale based on combo length
        newScale = Mathf.Min(newScale, maxScale); // Ensure the new scale does not exceed the maximum
        newScale += Random.Range(-randomScaleVariance, randomScaleVariance); // Add random variance to the scale
        transform.localScale = Vector3.one * newScale; // Apply the new scale to the enemy
    }

    public void AggroPlayer()
    {
        if (projectileEnemy)
        {
            StartCoroutine(BeginShoot());

            // Calculate direction from enemy to player (for facing)
            Vector3 directionToPlayer = player.position - transform.position;

            // Optional: Flatten the direction to avoid vertical tilting (if desired for gameplay)
            directionToPlayer.y = 0; // Comment this out if you want full 3D facing

            // Rotate to face the player
            transform.LookAt(transform.position + directionToPlayer);
                
            
            agent.isStopped = true;
        }
        else
        {
            agent.SetDestination(player.transform.position);
            agent.stoppingDistance = stoppingDistance;

            if (shouldCharge)
            {
                anim.SetBool("Run", !agent.isStopped);
            }
            else
            {
                anim.SetBool("Walk", !agent.isStopped);
            }

            

        }
    }

    private bool initialAggro = true;

    private float initialShootDelayMin = 1, initialShootDelayMax = 3;
    private IEnumerator BeginShoot()
    {
        while (true)
        {
            if (initialAggro == true)
            {
                float startWait = Random.Range(initialShootDelayMin, initialShootDelayMax);
                yield return new WaitForSeconds(startWait);
                anim.SetTrigger("Shoot");
                initialAggro = false;
            }
            float waitTime = Random.Range(shootIntervalMin, shootIntervalMax);
            yield return new WaitForSeconds(waitTime);
            anim.SetTrigger("Shoot");
        }
    }

    public void InstantiateProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, projectileShootPoint.position, Quaternion.identity);
        manager.Enemies.Add(projectile);
        projectile.GetComponent<EnemyProjectile>().manager = this.manager;
        projectile.GetComponent<EnemyProjectile>().enemy = this;
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

    private float timer;
    public float squareSuccessWindow = 0.5f;
    public bool startTimer, pOneSquare, pTwoSquare;
    
    
    void Update()
    {

        CheckAttackDist();
        

        if (startTimer)
        {
            timer += Time.deltaTime;
            
            if (pOneSquare == true && pTwoSquare == true) // both players pressed within the window
            {
                if (timer < squareSuccessWindow) // only succeed if still within the time window
                {
                    comboStep += 2;
                    playerScript.AddEpicness(playerScript.epicnessIncreasePerHit); // Increase epicness on successful combo input
                    playerScript.ShootMagicspell(this.transform, this.gameObject);
                    globalComboStarted = true;
                    localComboStarted = true;
                    //Debug.Log("Shooting from update");
                }
                else
                {
                    //Debug.Log("Too late for square input");
                }
                
                timer = 0;
                startTimer = false;
                pOneSquare = false;
                pTwoSquare = false;

                // Match non-square flow: release lock when the combo is completed.
                if (comboStep >= comboArray.Length)
                {
                    localComboStarted = false;
                    globalComboStarted = false;
                }

            }
            else if (timer >= squareSuccessWindow)
            {
                //Debug.Log("Resetting after time");
                timer = 0;
                startTimer = false;
                pOneSquare = false;
                pTwoSquare = false;

                // First player started sync, but partner missed the window.
                // Release ownership so players can retry this step.
                localComboStarted = false;
                globalComboStarted = false;

            }
        }
    }

    private bool SenderMatchesCurrentStep(int id)
    {
        var senderInfo = id == 1 ? playerOneInfo : playerTwoInfo;
        return senderInfo.symbOne == comboArray[comboStep] && senderInfo.symbTwo == comboArray[comboStep + 1];
    }
    
    private void CompareCombo(int id)
    {
        if (localComboStarted == false)
        {
            if (globalComboStarted)
            {
                return;
            }
        }
        
        // if combostep out of bounds return
        if (comboStep >= comboArray.Length)
        {
            return;
        }

        if (debug)
        {
            Debug.Log("ComboStep: " + comboStep);
            Debug.Log("Array symb one: " + comboArray[comboStep]);
            Debug.Log("Array symb two: " + comboArray[comboStep + 1]);
        }

        bool senderMatches = SenderMatchesCurrentStep(id);

        // While waiting for simultaneous input, only process this specific step.
        if (startTimer)
        {
            if (comboArray[comboStep] == 2 && senderMatches)
            {
                if (id == 1)
                    pOneSquare = true;
                else
                    pTwoSquare = true;
            }

            // Do not evaluate other combos or mark wrong input while sync is active.
            return;
        }
        
        if (senderMatches)
        {
            Debug.Log(comboStep);

            if (comboArray[comboStep] == 2) // If the top symbol is square
            {
                if (id == 1)
                    pOneSquare = true;
                else
                    pTwoSquare = true;

                // Lock this enemy as the active combo owner while waiting for partner input.
                localComboStarted = true;
                globalComboStarted = true;

                startTimer = true;
                timer = 0f;

                Debug.Log("returning");

                return;
            }

            Debug.Log("Shooting from Method");

            localComboStarted = true;
            globalComboStarted = true;

            playerScript.AddEpicness(playerScript.epicnessIncreasePerHit); // Increase epicness on successful combo input
            playerScript.ShootMagicspell(this.transform, this.gameObject); // Enemies are the only one that knows that they can be hit therefor is also the ones telling when the fireball should go off.
            //UpdateUI(); Vi opdatere istedet når fireball rammer enemy

            comboStep += 2;
            
            if (comboStep >= comboArray.Length)
            {
                localComboStarted = false;
                globalComboStarted = false;
            }
        }
        else
        {
            playerScript.AddEpicness(-playerScript.epicnessDecreasePerMiss); // Decrease epicness on failed combo input
            Debug.Log("Wrong input");
            wrongComboInput++;
        }
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
                    uiImage.GetComponent<Image>().sprite = moonImg; // Set the sprite to the Square image
                    break;
                case 2:
                    uiImage.GetComponent<Image>().sprite = starImg; // Set the sprite     to the Circle image
                    break;
                case 3:
                    uiImage.GetComponent<Image>().sprite = sunImg; // Set the sprite to the Triangle image
                    break;
            }
            contentSprite[i] = uiImage.GetComponent<Image>(); // Store the Image component in the contentSprite array
        }
    }

    public void CheatComboStep()
    {
        //UpdateUI();
        //comboStep += 2;
        playerScript.ShootMagicspell(this.transform, this.gameObject); // Enemies are the only one that knows that they can be hit therefor is also the ones telling when the fireball should go off.
        playerScript.AddEpicness(playerScript.epicnessIncreasePerHit); // Increase epicness on successful combo input
    }

    public void UpdateUI(bool instantKill = false)
    {
       // animate the UI elements with shake effect and then disable the current combo step's UI elements
        int top = uiComboStep;
        int bottom = uiComboStep + 1;

        uiComboStep += 2;

        // If we've already consumed all combo inputs, complete immediately.
        if (bottom >= comboArray.Length)
        {
            return;
        }

        if (bottom >= contentSprite.Length || top >= contentSprite.Length)
        {
            return;
        }

        // ----------- //
        // for bedre visual cues
        canvasComponent.sortingOrder = 100; //TODO, Måske slet fordi det fucker projectile visibility up 
        enemySpotLight.DOIntensity(lightFeedbackIntensity, visualFeedbackFadeDuration);
        canvasImage.color = Color.white; // for at gøre den mere tydlig
        canvasImage.DOFade(1, visualFeedbackFadeDuration); // sætter alpha til 1 (sætter den ned igen i CheckComboCompletion når combo er færdig)
        // ----------- //

        Sequence comboStepSequence = DOTween.Sequence();
        comboStepSequence.Join(
            contentSprite[bottom].transform
                .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false)
        );
        
        comboStepSequence.Join(
            contentSprite[top].transform
                .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false)
        );
        
        agent.speed = 0;
        anim.SetTrigger("Hit");

        comboStepSequence.OnComplete(() =>
        {
            contentSprite[bottom].enabled = false;
            contentSprite[top].enabled = false;
            //comboStep++; // Move to the next step in the combo sequence
            CheckComboCompletion(instantKill);
            comboStepSequence.Kill();

        });
    }

    public void HitRecoverAnimEvent()
    {
            agent.speed = speed;
    }

    private void CheckComboCompletion(bool instantKill)
    {
        int totalSteps = comboArray.Length;
        if (uiComboStep >= totalSteps || instantKill == true)
        {
            canvasImage.DOFade(lastCanvasImageAlpha, visualFeedbackFadeDuration);
            enemySpotLight.DOIntensity(0, visualFeedbackFadeDuration);

            Die();
        }
    }

    private void CheckAttackDist()
    {
        if (player == null || agent == null) return;
        
        if (Vector3.Distance(transform.position, player.transform.position) <= agent.stoppingDistance + 1)
        {
            anim.SetBool("Attack", true);
            
            /*
            currentattackCooldown -= Time.deltaTime; // Decrease the cooldown timer by the time elapsed since the last frame
            
            if (currentattackCooldown <= 0f)
            {
                Debug.Log("Player Damaged!"); // Player is damaged
                playerScript.TakeDamage(damageAmount); // Call the TakeDamage method on the player script
                currentattackCooldown = attackCooldown; // Reset the cooldown timer
            }
            */
        }
    }

    public void DamagePlayer()
    {
        Debug.Log("Player Damaged!"); // Player is damaged
        playerScript.TakeDamage(damageAmount); // Call the TakeDamage method on the player script
        //currentattackCooldown = attackCooldown; // Reset the cooldown timer
    }

    private void Die()
    {
        if (isDead) return; // Prevent multiple death triggers
            isDead = true;

        startTimer = false;
        pOneSquare = false;
        pTwoSquare = false;
        timer = 0f;
        globalComboStarted = false;
        localComboStarted = false;
        Debug.Log("Enemy Defeated!"); // Enemy is defeated
        manager.Enemies.Remove(this.gameObject);
        manager.EnemyDied();
        anim.SetBool("Dead", true);
        agent.speed = 0;
        canvas.SetActive(false);
        //Destroy(gameObject); // Destroy the enemy game object
        Invoke(nameof(RM), 2f);
    }

    void RM()
    {
        Destroy(this.gameObject);
    }

    
}