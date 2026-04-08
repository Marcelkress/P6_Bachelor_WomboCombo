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

    public float UIFadeTime = 0.8f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameOverText.DOFade(0, 0);
        gameOverBackground.DOFade(0, 0);
        restartButton.SetActive(false);
    }

    public void ShowGameOverMenu()
    {
        //gameOverBackground.gameObject.SetActive(true);
        gameOverText.DOFade(1, UIFadeTime);
        gameOverBackground.DOFade(1, UIFadeTime).OnComplete(() =>
        {
            restartButton.SetActive(true);
        });
        
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
