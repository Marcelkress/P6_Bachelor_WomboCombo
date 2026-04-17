using UnityEngine;
using PathCreation.Examples;
using DG.Tweening;
using UnityEngine.AI;
using UnityEngine.UI;

public class BossComboSystem : MonoBehaviour
{
    private Transform player;
    private EpicBossLogic epicBossLogic;
    [SerializeField] private Animator anim;


    public int[] bossComboArray; // health

    [Header("Combo")]
    [SerializeField] private int comboStep = 0;
    private int uiComboStep;
    private Image[] contentSprite;

    [Header("UI")]
    [SerializeField] private Transform content;
    public Sprite SquareImage, CircleImage, TriangleImage;
    public GameObject inputUIImage;
    public float shakeDuration = 0.2f;
    public Vector3 shakeStrength = new Vector3(10f, 10f, 0f);
    public float shakeRandomness = 90f;
    public int shakeVibrato = 10;


    public bool debug = false; // Debugging tool for combo steps and symbols, set to true to enable debug logs in CompareCombo method.
    // Combo check synchronization.
    private static bool globalComboStarted;
    private bool localComboStarted;

    private Player playerScript;
    private PlayerInfoStruct playerOneInfo;
    private PlayerInfoStruct playerTwoInfo;

    private float timer;
    public float squareSuccessWindow = 0.5f;
    public bool startTimer;
    public bool pOneSquare;
    public bool pTwoSquare;

    private void SetComboLock(bool locked)
    {
        localComboStarted = locked;
        globalComboStarted = locked;
    }

    private void ResetSquareWindowState()
    {
        timer = 0f;
        startTimer = false;
        pOneSquare = false;
        pTwoSquare = false;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (player == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                player = playerGo.transform;
            }
        }

        epicBossLogic = GetComponent<EpicBossLogic>();
        playerScript = player.GetComponent<Player>();

        bossComboArray = RandomArray(4, 8); // random combo array for boss, can be used in phase 1 for example
        
        InitializeUI();

        if (InputManager.instance != null)
        {
            InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
            InputManager.instance.PlayerTwoEvent.AddListener(PlayerTwoUpdate);
        }

    }

    private void OnDestroy()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.PlayerOneEvent.RemoveListener(PlayerOneUpdate);
            InputManager.instance.PlayerTwoEvent.RemoveListener(PlayerTwoUpdate);
        }

        if (localComboStarted)
            SetComboLock(false);

        ResetSquareWindowState();

    }

    private void Update()
    {
        if (startTimer)
        {
            timer += Time.deltaTime;

            if (pOneSquare && pTwoSquare)
            {
                if (timer < squareSuccessWindow)
                {
                    comboStep += 2;
                    epicBossLogic.bossGothitted = true;
                    if (playerScript != null)
                    {
                        playerScript.ShootMagicspell(transform, gameObject);
                    }

                    if (comboStep >= bossComboArray.Length)
                        SetComboLock(false);
                }
                else
                {
                    SetComboLock(false);
                }

                ResetSquareWindowState();
            }
            else if (timer >= squareSuccessWindow)
            {
                SetComboLock(false);
                ResetSquareWindowState();
            }
        }
    }


    public int[] RandomArray(int minLength, int maxLength)
    {

        int length = Random.Range(minLength * 2, maxLength * 2); // Ensure the length is even Ganger med 2 for at gøre det mere intuitivt i inspector
        
        if (length % 2 != 0) // Sikrer det er et lige tal
        {
            length += 1;
        }
        
        int[] randArray = new int[length];
        
        for (int i = 0; i < randArray.Length; i++)
        {
            randArray[i] = Random.Range(1, 4);
        }
        
        return randArray;
    }

    public void PlayerOneUpdate()
    {
        if (!epicBossLogic.minionsCleared || InputManager.instance == null)
        {
            return;
        }

        playerOneInfo = InputManager.instance.GetPlayerSymbols(1);
        CompareCombo(1);
    }

    public void PlayerTwoUpdate()
    {
        if (!epicBossLogic.minionsCleared || InputManager.instance == null)
        {
            return;
        }

        playerTwoInfo = InputManager.instance.GetPlayerSymbols(2);
        CompareCombo(2);
    }


     private void CompareCombo(int id)
    {
        if (!epicBossLogic.minionsCleared)
        {
            return;
        }

        if (!localComboStarted && globalComboStarted)
            return;

        if (comboStep >= bossComboArray.Length)
        {
            if (localComboStarted)
                SetComboLock(false);
            return;
        }

        if (debug)
        {
            Debug.Log("ComboStep: " + comboStep);
            Debug.Log("Array symb one: " + bossComboArray[comboStep]);
            Debug.Log("Array symb two: " + bossComboArray[comboStep + 1]);
        }

            // If either player one or player two symbols are correct continue
        if ((playerOneInfo.symbOne == bossComboArray[comboStep] && playerOneInfo.symbTwo == bossComboArray[comboStep + 1])
            || (playerTwoInfo.symbOne == bossComboArray[comboStep] && playerTwoInfo.symbTwo == bossComboArray[comboStep + 1]))
        {
            Debug.Log(comboStep);

            if (bossComboArray[comboStep] == 2) // If the top symbol is square
            {
                if (playerOneInfo.symbOne == bossComboArray[comboStep] && id == 1)
                    pOneSquare = true;

                if(playerTwoInfo.symbOne == bossComboArray[comboStep] && id == 2)
                    pTwoSquare = true;

                SetComboLock(true);
                startTimer = true;

                Debug.Log("returning");

                return;
            }

            Debug.Log("Shooting from Method");

            SetComboLock(true);

            if (playerScript != null)
            {
                playerScript.ShootMagicspell(this.transform, this.gameObject); 
            }
            //UpdateUI(); Vi opdatere istedet når fireball rammer enemy

            comboStep += 2;

            

            if (comboStep >= bossComboArray.Length)
            {
                SetComboLock(false);
            }
        }
    }
    
    private void InitializeUI()
    {
        if (content == null || inputUIImage == null || bossComboArray == null)
        {
            return;
        }

        contentSprite = new Image[bossComboArray.Length]; // Initialize the contentSprite array to match the length of comboArray

        for (int i = 0; i < bossComboArray.Length; i++)
        {
            // Instantiate a new UI image for each combo step and set its parent to the content transform
            GameObject uiImage = Instantiate(inputUIImage, content);

            // 1 = Square, 2 = Circle, 3 = Triangle (you can customize this mapping as needed)
            switch (bossComboArray[i])
            {
                case 1: 
                    uiImage.GetComponent<Image>().sprite = TriangleImage; // Set the sprite to the Square image
                    break;
                case 2:
                    uiImage.GetComponent<Image>().sprite = SquareImage; // Set the sprite     to the Circle image
                    break;
                case 3:
                    uiImage.GetComponent<Image>().sprite = CircleImage; // Set the sprite to the Triangle image
                    break;
            }
            contentSprite[i] = uiImage.GetComponent<Image>(); // Store the Image component in the contentSprite array
        }
    }

     public void CheatComboStep()
    {
        if (!epicBossLogic.minionsCleared) return;

        epicBossLogic.minionsCleared = false;
        epicBossLogic.bossGothitted = true;
        UpdateUI();
        comboStep += 2;
        
    }

    public void UpdateUI()
    {
        if (contentSprite == null || contentSprite.Length == 0)
        {
            return;
        }

       // animate the UI elements with shake effect and then disable the current combo step's UI elements
        int top = uiComboStep;
        int bottom = uiComboStep + 1;

        uiComboStep += 2;

        // If we've already consumed all combo inputs, complete immediately.
        if (bottom >= bossComboArray.Length)
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

        if (anim != null)
        {
            anim.SetTrigger("Hit");
        }

        comboStepSequence.OnComplete(() =>
        {
            contentSprite[bottom].enabled = false;
            contentSprite[top].enabled = false;

            //comboStep++; // Move to the next step in the combo sequence
            CheckComboCompletion();
            comboStepSequence.Kill();
        });
    }

     private void CheckComboCompletion()
    {
        if (bossComboArray == null)
        {
            return;
        }

        if (uiComboStep >= bossComboArray.Length)
        {
            Debug.Log("Boss combo completed. Advancing to phase 2.");
            epicBossLogic.StartPhaseTwo();
        }
    }


}
