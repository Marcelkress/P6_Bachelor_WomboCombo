using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Image gameOverBackground;
    public GameObject restartButton;
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

    public void RespawnFade()
    {
        gameOverBackground.gameObject.SetActive(true);
        gameOverText.gameObject.SetActive(true);
        Debug.Log("HALLO?!?!?");
        //gameOverBackground.gameObject.SetActive(true);
        gameOverText.DOFade(1, UIFadeTime);
        gameOverBackground.DOFade(1, UIFadeTime).OnComplete(() =>
        {
            player.TakeDamage(0); // Force health bar update and shake effect
            encounterManager.RestartFromLastRespawn();

            gameOverText.DOFade(0, UIFadeTime);
            gameOverBackground.DOFade(0, UIFadeTime).OnComplete(() =>
            {
                gameOverText.gameObject.SetActive(false);
                gameOverBackground.gameObject.SetActive(false);
            });
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
}
