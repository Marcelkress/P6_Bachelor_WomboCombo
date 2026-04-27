using UnityEngine;

public enum TutorialType
{
    NormalControllers,
    CustomControllers
}
public class TutorialSwitcher : MonoBehaviour
{
    [Header("Switch Tutorial Type")]
    [Tooltip("Select the tutorial type to display at the start of the game.")]
    [SerializeField] private TutorialType _currentTutorialType;


    [Header("Tutorial GameObjects")]
    public GameObject ControllersTutorial;
    public GameObject CustomControllersTutorial;

    public CanvasGroup ControllerVisuals;

    private void Start()
    {
        switch (_currentTutorialType)
        {
            case TutorialType.NormalControllers:
                ControllersTutorial.gameObject.SetActive(true);
                ControllerVisuals.alpha = 1f;
                CustomControllersTutorial.gameObject.SetActive(false);
                break;
            case TutorialType.CustomControllers:
                ControllersTutorial.gameObject.SetActive(false);
                ControllerVisuals.alpha = 0f;
                CustomControllersTutorial.gameObject.SetActive(true);
                break;
        }
    }
}
