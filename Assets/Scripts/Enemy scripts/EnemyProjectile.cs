using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EnemyProjectile : MonoBehaviour, IEnemyDamagable
{
    private Transform target;
    public int damage = 1;
    public float speedTime;
    public float liveTime = 30f;
    private float timer;

    public Transform playerSpellTargetPoint;
    
    [Header("Combo stuff")]
    public int[] comboArray;
    [SerializeField] private int comboStep = 0;
    private int uiComboStep;
    [HideInInspector] public int comboLength;
    public int minLength = 1, maxLength = 2;
    private float deathTimer;
    public float squareSuccessWindow = 0.5f;
    public bool startTimer, pOneStar, pTwoStar;

    
    [Header("UI")] 
    [SerializeField] private Transform content;
    public GameObject canvas;
    public float canvasFadeDuration = 0.1f;
    public Sprite moonImg, starImg, sunImg; // references to the UI images for each button (Square, Circle, Triangle)
    public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)
    public float shakeDuration = 0.2f;
    public Vector3 shakeStrength;
    public float shakeRandomness = 10;
    public int shakeVibrato = 10;

    public EnemyManager manager;
    private PlayerInfoStruct playerOneInfo, playerTwoInfo;
    private Transform player;
    private Player playerScript;

    public Enemy enemy;

    private Canvas canvasComponent;
    private Image canvasImage;
    private float lastCanvasImageAlpha;
    
    
    [SerializeField] private AK.Wwise.Event enemyProjectileSound;
    [SerializeField] private AK.Wwise.Event enemyProjectileHitSound;
  
    
    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform; // Find the player by tag
        playerScript = player.GetComponent<Player>();
        InitializeCombo();
        InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
        InputManager.instance.PlayerTwoEvent.AddListener(PlayerTwoUpdate);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasComponent = canvas.GetComponent<Canvas>();
        canvasImage = canvas.GetComponentInChildren<Image>();
        lastCanvasImageAlpha = canvasImage.color.a;
        target = Camera.main.transform;
        transform.DOMove(target.position, speedTime, false).SetEase(Ease.InOutCubic);
        InitializeUI();
        enemyProjectileSound.Post(gameObject);
    }

    private void OnDisable()
    {
        InputManager.instance.PlayerOneEvent.RemoveListener(PlayerOneUpdate);
        InputManager.instance.PlayerTwoEvent.RemoveListener(PlayerTwoUpdate);
       
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            other.transform.GetComponent<Player>().TakeDamage(damage);
            enemyProjectileHitSound.Post(this.gameObject);
            Die();
            enemyProjectileSound.Stop(this.gameObject);
           
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
                if (timer < squareSuccessWindow) // only succeed if still within the time window
                {
                    comboStep += 2;
                    playerScript.ShootMagicspell(playerSpellTargetPoint, this.gameObject, true);
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
            else if (timer >= squareSuccessWindow)
            {
                //Debug.Log("Resetting after time");
                timer = 0;
                startTimer = false;
                pOneStar = false;
                pTwoStar = false;
            }
        }

        deathTimer += Time.deltaTime;
        if (deathTimer > liveTime)
        {
            Die();
        }
    }

    public void CheatComboStep()
    {
        //UpdateUI();
        //comboStep += 2;
        playerScript.ShootMagicspell(playerSpellTargetPoint, this.gameObject, true); // Enemies are the only one that knows that they can be hit therefor is also the ones telling when the fireball should go off.

    }
    
    private bool hasStarted = false;
    
    private void CompareCombo(int id)
    {
        // if combostep out of bounds return
        if (comboStep >= comboArray.Length) // Det er bare for at prevent fejl errors når vi spiller
        {
            return;
        }

        //Debug.Log("Combo on projectile");
        Debug.Log(comboStep);
        // If either player one or player two symbols are correct continue
        if ((playerOneInfo.symbOne == comboArray[comboStep] && playerOneInfo.symbTwo == comboArray[comboStep + 1])
            || (playerTwoInfo.symbOne == comboArray[comboStep] && playerTwoInfo.symbTwo == comboArray[comboStep + 1]))
        {
            //Debug.Log(comboStep);

            if (comboArray[comboStep] == 2) // If the top symbol is star
            {
                if (playerOneInfo.symbOne == comboArray[comboStep] && id == 1)
                    pOneStar = true;

                if(playerTwoInfo.symbOne == comboArray[comboStep] && id == 2)
                    pTwoStar = true;

                startTimer = true;

                //Debug.Log("Starting time");

                return;
            }

            // Debug.Log("Shooting from Method");
            playerScript.ShootMagicspell(playerSpellTargetPoint, this.gameObject, true);
            comboStep += 2;
        }
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

    private void InitializeCombo()
    {
        int length = UnityEngine.Random.Range(minLength * 2, maxLength * 2); // Ensure the length is even Ganger med 2 for at gøre det mere intuitivt i inspector
        
        if (length % 2 != 0) // Sikrer det er et lige tal
        {
            length += 1;
        }
        
        int[] randArray = new int[length];
        
        for (int i = 0; i < randArray.Length; i++)
        {
            randArray[i] = UnityEngine.Random.Range(1, 3);
        }

        comboArray = randArray;
    }
    
    private Image[] contentSprite;
    
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

        // ------ //
        // for nemmere at se hvilken enemy man er i gang med
        canvasComponent.sortingOrder = 100;
        canvasImage.DOFade(1, canvasFadeDuration); // sætter alpha til 1 (sætter den ned igen i CheckComboCompletion når combo er færdig)
        // ------ //

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
            contentSprite[bottom].enabled = false;
            contentSprite[top].enabled = false;

            //comboStep++; // Move to the next step in the combo sequence
            CheckComboCompletion(instantKill);
            comboStepSequence.Kill();
        });
    }
    
    private void CheckComboCompletion(bool instantKill)
    {
        int totalSteps = comboArray.Length;
        if (uiComboStep >= totalSteps || instantKill)
        {
            canvasImage.DOFade(lastCanvasImageAlpha, canvasFadeDuration);
            enemyProjectileSound.Stop(this.gameObject);
            Die();
            
        }
    }
    /*

    private void OnDisable()
    {
        Die();
    }
    */
    private bool isDead;
    private void Die()
    {
        if (isDead) return; // Prevent multiple death triggers
            isDead = true;
        
        manager.Enemies.Remove(this.gameObject);
        manager.EnemyDied();
        enemyProjectileSound.Stop(this.gameObject);
        Destroy(this.gameObject);
        
    }
    

    
    
}
