using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public Image gameOverBackground;
    public GameObject restartButton;
    public CanvasGroup respawnUI;
    public TMP_Text gameOverText;

    public EncounterManager encounterManager;

    public float UIFadeTime = 0.8f;

    public GameObject startScreen;

    public bool removeControllerUI = false;
    public bool removeDebugUI = false;
    public GameObject debugUI;
    public GameObject[] controllerUI;

    [SerializeField] private Player player;
    public bool stopTime = false;
    public Image[] livesUI;

    private PlayerInput playerInput; // Reference to the PlayerInput component for handling input actions
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player != null)
        {
            player.livesChangedEvent.AddListener(RemoveLifeUI);
        }
        if (removeDebugUI)
        {
            debugUI.SetActive(false);
        }
        if (removeControllerUI)
        {
            foreach (var ui in controllerUI)
            {
                ui.SetActive(false);
            }
        }
        if (stopTime == true)
        {
            startScreen.SetActive(true);
        }

       
        if (stopTime)
        {
            Time.timeScale = 0f;
        }
        gameOverText.DOFade(0, 0);
        gameOverBackground.DOFade(0, 0);
        restartButton.SetActive(false);


        if (InputManager.instance != null)
        {
            InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
            InputManager.instance.PlayerTwoEvent.AddListener(PlayerTwoUpdate);
        }
    }

    private void RemoveLifeUI()
    {
        int livesLeft = player.lives;
        for (int i = 0; i < livesUI.Length; i++)
        {
            if (i < livesLeft)
            {
                livesUI[i].enabled = true;
            }
            else
            {
                livesUI[i].enabled = false;
            }
        }
    }

    // Bare så vi kan starte spillet når vi er klar
    public void StartGame()
    {
        Time.timeScale = 1f;
        startScreen.SetActive(false);
    }

    bool waitingForContinue = false;

    public void RespawnFade()
    {
        gameOverBackground.gameObject.SetActive(true);
        gameOverText.gameObject.SetActive(true);

        respawnUI.alpha = 1f;
        Debug.Log("HALLO?!?!?");
        //gameOverBackground.gameObject.SetActive(true);
        gameOverText.DOFade(1, UIFadeTime);



        gameOverBackground.DOFade(1, UIFadeTime).OnComplete(() =>
        {


            waitingForContinue = true;
        });
    }

    public void GameOverFade()
    {
        gameOverBackground.gameObject.SetActive(true);
        gameOverText.gameObject.SetActive(true);
        gameOverText.DOFade(1, UIFadeTime);
        gameOverBackground.DOFade(1, UIFadeTime).OnComplete(() =>
        {
            restartButton.SetActive(true);
        });
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private PlayerInfoStruct playerOneInfo, playerTwoInfo;

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

    private int topValue = 2;
    private int bottomValue = 2;

    private bool pOneStar = false, pTwoStar = false;
    private bool startTimer = false, succesfullComboDone = false;
    private void CompareCombo(int id)
    {
        
        if (waitingForContinue == false)
            return;


        // If either player one or player two symbols are correct continue
        if ((playerOneInfo.symbOne == topValue && playerOneInfo.symbTwo == bottomValue)
            || (playerTwoInfo.symbOne == topValue && playerTwoInfo.symbTwo == bottomValue))
        {
            //Debug.Log(comboStep);

           
                if (playerOneInfo.symbOne == topValue && id == 1)
                    pOneStar = true;
                    

                if(playerTwoInfo.symbOne == topValue && id == 2)
                    pTwoStar = true;

                startTimer = true;
                succesfullComboDone = false;

                Debug.Log("returning");

        }
    }


    private float timer = 0f;

    private float starSuccessWindow = 0.5f; // Time window for both players to successfully input the combo
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
                    waitingForContinue = false;
                    ContinueRespawn();
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

    private void ContinueRespawn()
    {
        player.TakeDamage(0); // Force health bar update and shake effect
        encounterManager.RestartFromLastRespawn();

        respawnUI.alpha = 0f;
        gameOverText.DOFade(0, UIFadeTime);
        gameOverBackground.DOFade(0, UIFadeTime).OnComplete(() =>
        {
            gameOverText.gameObject.SetActive(false);
            gameOverBackground.gameObject.SetActive(false);
        });
    }

}
