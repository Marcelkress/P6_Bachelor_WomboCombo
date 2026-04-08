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

    public bool debug = false;
    // ComboCheck
    private static bool globalComboStarted;
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

    [Header("Animation")] public Animator anim;
    
    
    private PlayerInfoStruct playerOneInfo, playerTwoInfo;

    [HideInInspector] public EnemyManager manager;


    private float speed;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        comboLength = comboArray.Length / 2;
        speed = agent.speed;
        InitializeUI(); 
        
        InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
        InputManager.instance.PlayerTwoEvent.AddListener(PlayerTwoUpdate);
        
        SetRandomColor();
    }

    void SetRandomColor()
    {
        // Clone the material so each enemy has its own instance
        enemyMaterial = new Material(enemyMaterial);
        enemyMeshRenderer.material = enemyMaterial;

        enemyMaterial.SetColor("_LitColor", Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f));
        Color color = enemyMaterial.GetColor("_LitColor");
        enemyMaterial.SetColor("_ShadowColor", color * 0.5f);

    }

    public void Initialize(float aggroDelay)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform; // Find the player by tag
        playerScript = player.GetComponent<Player>();
        //Debug.Log(aggroDelay);
        Invoke(nameof(AggroPlayer), aggroDelay);
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
        }
    }

    private IEnumerator BeginShoot()
    {
        while (true)
        {
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
        
        if(!projectileEnemy)
            anim.SetBool("Walk", !agent.isStopped);

        if (startTimer)
        {
            timer += Time.deltaTime;
            
            if (pOneSquare == true && pTwoSquare == true) // both players pressed within the window
            {
                if (timer < squareSuccessWindow) // only succeed if still within the time window
                {
                    comboStep += 2;
                    playerScript.ShootFireball(this.transform, this.gameObject);
                    //Debug.Log("Shooting from update");
                }
                else
                {
                    //Debug.Log("Too slow - resetting");
                }
                
                timer = 0;
                startTimer = false;
                pOneSquare = false;
                pTwoSquare = false;
            }
            else if (timer >= squareSuccessWindow)
            {
                //Debug.Log("Resetting after time");
                timer = 0;
                startTimer = false;
                pOneSquare = false;
                pTwoSquare = false;
            }
        }
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

        if (debug)
        {
            Debug.Log("ComboStep: " + comboStep);
            Debug.Log("Array symb one: " + comboArray[comboStep]);
            Debug.Log("Array symb two: " + comboArray[comboStep + 1]);
        }

            // If either player one or player two symbols are correct continue
        if ((playerOneInfo.symbOne == comboArray[comboStep] && playerOneInfo.symbTwo == comboArray[comboStep + 1])
            || (playerTwoInfo.symbOne == comboArray[comboStep] && playerTwoInfo.symbTwo == comboArray[comboStep + 1]))
        {
            Debug.Log(comboStep);

            if (comboArray[comboStep] == 2) // If the top symbol is square
            {
                if (playerOneInfo.symbOne == comboArray[comboStep] && id == 1)
                    pOneSquare = true;

                if(playerTwoInfo.symbOne == comboArray[comboStep] && id == 2)
                    pTwoSquare = true;

                startTimer = true;

                Debug.Log("returning");

                return;
            }

            Debug.Log("Shooting from Method");

            localComboStarted = true;
            globalComboStarted = true;

            playerScript.ShootFireball(this.transform, this.gameObject); // Enemies are the only one that knows that they can be hit therefor is also the ones telling when the fireball should go off.
            //UpdateUI(); Vi opdatere istedet når fireball rammer enemy

            comboStep += 2;
            
            if (comboStep >= comboArray.Length)
            {
                localComboStarted = false;
                globalComboStarted = false;
            }
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
        UpdateUI();
        comboStep += 2;
    }

    public void UpdateUI()
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
        GetComponentInChildren<Canvas>().sortingOrder = 100;
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
            CheckComboCompletion();
            comboStepSequence.Kill();
        });
    }

    public void HitRecoverAnimEvent()
    {
            agent.speed = speed;
    }

    private void CheckComboCompletion()
    {
        int totalSteps = comboArray.Length;
        if (uiComboStep >= totalSteps)
        {
            Die();
        }
    }

    private void CheckAttackDist()
    {
        
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