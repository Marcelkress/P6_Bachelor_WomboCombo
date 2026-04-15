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
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startScreen.SetActive(true);
        Time.timeScale = 0f;
        gameOverText.DOFade(0, 0);
        gameOverBackground.DOFade(0, 0);
        restartButton.SetActive(false);
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
