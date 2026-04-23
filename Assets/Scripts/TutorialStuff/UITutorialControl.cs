using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class UITutorialControl : MonoBehaviour, IPointerClickHandler
{
    
    public int topSymbol = 1;
    public int bottomSymbol = 1;

    public CanvasGroup nextUI;
    public GameObject currentUI;
    private CanvasGroup currentCanvasGroup;

    public Color correctColor = Color.green;

    public Color incorrectColor = Color.red;
    private PlayerInfoStruct playerOneInfo, playerTwoInfo;

    public Sprite moonImg, starImg, sunImg; // references to the UI images for each button (Square, Circle, Triangle)

    public Image Top;
    public Image Bottom;

    public Image fadeToBlackImage;


    public bool shouldLoadNextScene = false;
    public string nextSceneName = "scene1";

    private bool correctSymbol = false;


    public bool useMoreSymbolsInputs = false;

    public CanvasGroup[] symbolUIElements;
    public int[] symbolInputs;

    public int currentComboIndex = 0;
    public int UIComboIndex = 0;

    private static bool sceneLoadedAsync = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentCanvasGroup = currentUI.GetComponent<CanvasGroup>();
        nextUI.alpha = 0;
        nextUI.interactable = false;
        nextUI.blocksRaycasts = false;

        nextUI.gameObject.SetActive(false);

        if (InputManager.instance != null)
        {
            InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
            InputManager.instance.PlayerTwoEvent.AddListener(PlayerTwoUpdate);
        }

        InitializeUI();
        
    }

    private void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.PlayerOneEvent.RemoveListener(PlayerOneUpdate);
            InputManager.instance.PlayerTwoEvent.RemoveListener(PlayerTwoUpdate);
        }
    }

    public void PlayerOneUpdate()
    {
        playerOneInfo = InputManager.instance.GetPlayerSymbols(1);
        CheckSymbol(1);
    }
    public void PlayerTwoUpdate()
    {
        playerTwoInfo = InputManager.instance.GetPlayerSymbols(2);
        CheckSymbol(2);
    }

    private float timer;
    private float squareSuccessWindow = 0.5f; // Time window for successful combo input
    private void Update()
    {
        if (startTimer)
        {
            timer += Time.deltaTime;
            
            if (pOneStar == true && pTwoStar == true) // both players pressed within the window
            {
                if (timer < squareSuccessWindow) // only succeed if still within the time window
                {
                symbolUIElements[UIComboIndex].GetComponent<Image>().DOColor(correctColor, 0.1f).OnComplete(() =>
                {
                    symbolUIElements[UIComboIndex].DOFade(0, 0.5f).OnComplete(() =>
                    {
                        symbolUIElements[UIComboIndex].gameObject.SetActive(false);
                        currentComboIndex += 2;
                        UIComboIndex++;
                    });
                });

                }
                else
                {
                    //Debug.Log("Too late for square input");
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
        if (startTimerSingle)
        {
            timer += Time.deltaTime;
            
            if (pOneStar == true && pTwoStar == true) // both players pressed within the window
            {
                if (timer < squareSuccessWindow) // only succeed if still within the time window
                {
                    if (shouldLoadNextScene)
                    {
                        LoadScene(nextSceneName);
                        return;
                    }
                    NextUI();
                }
                else
                {
                    //Debug.Log("Too late for square input");
                }
                
                timer = 0;
                startTimerSingle = false;
                pOneStar = false;
                pTwoStar = false;

            }
            else if (timer >= squareSuccessWindow)
            {
                //Debug.Log("Resetting after time");
                timer = 0;
                startTimerSingle = false;
                pOneStar = false;
                pTwoStar = false;

            }
        }
    }


    private bool pOneStar = false;
    private bool pTwoStar = false;
    private bool startTimer = false;
    private bool startTimerSingle = false;

    void CheckSymbol(int id)
    {
        if (useMoreSymbolsInputs)
        {
            if (currentComboIndex >= symbolInputs.Length)
            {
                return; // All combos have been checked
            }
        }
        
    
        if (useMoreSymbolsInputs)
        {
            if (playerOneInfo.symbOne == symbolInputs[currentComboIndex] && playerOneInfo.symbTwo == symbolInputs[currentComboIndex+1] 
            || playerTwoInfo.symbOne == symbolInputs[currentComboIndex] && playerTwoInfo.symbTwo == symbolInputs[currentComboIndex+1])
            {
               
                if (symbolInputs[currentComboIndex] == 2 ) // if top symbol is star
                {
                    if (playerOneInfo.symbOne == symbolInputs[currentComboIndex] && id == 1)
                    {
                        pOneStar = true;
                    }
                    if (playerTwoInfo.symbOne == symbolInputs[currentComboIndex] && id == 2)
                    {
                        pTwoStar = true;
                    }

                    startTimer = true;
                    return;
                }

                symbolUIElements[UIComboIndex].GetComponent<Image>().DOColor(correctColor, 0.1f).OnComplete(() =>
                {
                    symbolUIElements[UIComboIndex].DOFade(0, 0.5f).OnComplete(() =>
                    {
                        symbolUIElements[UIComboIndex].gameObject.SetActive(false);

                        currentComboIndex += 2;
                        UIComboIndex++;

                        if (currentComboIndex >= symbolInputs.Length)
                        {
                                if (shouldLoadNextScene)
                                {
                                    LoadScene(nextSceneName);
                                    return;
                                }
                            DOTween.KillAll(); // kill all tweens to prevent overlap

                            NextUI();
                        }

                    });
                });
                
                
               
                
            }
            else
            {
                symbolUIElements[UIComboIndex].GetComponent<Image>().DOColor(incorrectColor, 0.2f);
                symbolUIElements[UIComboIndex].GetComponent<RectTransform>().DOShakeAnchorPos(0.2f, 10, 20).OnComplete(() =>
                {
                    symbolUIElements[UIComboIndex].GetComponent<Image>().DOColor(Color.white, 0.1f);
                });
            }

            return;
        }

        
        if (playerOneInfo.symbOne == topSymbol && playerOneInfo.symbTwo == bottomSymbol 
        || playerTwoInfo.symbOne == topSymbol && playerTwoInfo.symbTwo == bottomSymbol)
        {
            if (topSymbol == 2 ) // if top symbol is star
            {
                   
                if (playerOneInfo.symbOne == topSymbol && id == 1)
                {
                    pOneStar = true;
                }
                if (playerTwoInfo.symbOne == topSymbol && id == 2)
                {
                    pTwoStar = true;
                }

                startTimerSingle = true;
                return;
            }
            Debug.Log("Correct symbol input detected!");
            if (shouldLoadNextScene)
            {
                LoadScene(nextSceneName);
                return;
            }
            NextUI();
        }
        else
        {
            this.gameObject.GetComponent<Image>().DOColor(incorrectColor, 0.2f);
            this.gameObject.GetComponent<RectTransform>().DOShakeAnchorPos(0.2f, 10, 20).OnComplete(() =>
            {
                this.gameObject.GetComponent<Image>().DOColor(Color.white, 0.1f);
            });
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shouldLoadNextScene)
            {
                LoadScene(nextSceneName);
                return;
            }
        NextUI();
    }

    void NextUI()
    {   
        nextUI.gameObject.SetActive(true);
        // tween the canvas group to fade in
        nextUI.DOFade(1, 1f);

        
        currentCanvasGroup.DOFade(0, 1f).OnComplete(() =>
        {
            nextUI.interactable = true;
            nextUI.blocksRaycasts = true;
            currentCanvasGroup.interactable = false;
            currentCanvasGroup.blocksRaycasts = false;
            currentUI.SetActive(false);
        });
            
        
    }

    void LoadScene(string sceneName)
    {
        fadeToBlackImage.gameObject.SetActive(true);

        fadeToBlackImage.DOFade(1, 1f).OnComplete(() =>
        {
            AsyncSceneLoader.instance.StartLoadedScene();   
        });
       
    }

    

     private void InitializeUI()
    {

        
            // 1 = Square, 2 = Circle, 3 = Triangle (you can customize this mapping as needed)
            switch (topSymbol)
            {
                case 1: 
                    Top.GetComponent<Image>().sprite = moonImg; // Set the sprite to the Square image
                    break;
                case 2:
                    Top.GetComponent<Image>().sprite = starImg; // Set the sprite     to the Circle image
                    break;
                case 3:
                    Top.GetComponent<Image>().sprite = sunImg; // Set the sprite to the Triangle image
                    break;
            }
            switch (bottomSymbol)
            {
                case 1: 
                    Bottom.GetComponent<Image>().sprite = moonImg; // Set the sprite to the Square image
                    break;
                case 2:
                    Bottom.GetComponent<Image>().sprite = starImg; // Set the sprite     to the Circle image
                    break;
                case 3:
                    Bottom.GetComponent<Image>().sprite = sunImg; // Set the sprite to the Triangle image
                    break;
            }
            
        
    }


}
