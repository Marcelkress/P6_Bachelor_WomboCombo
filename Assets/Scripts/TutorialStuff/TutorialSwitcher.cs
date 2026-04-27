using UnityEngine;

public enum TutorialType
{
    Controllers,
    CustomControllers
}
public class TutorialSwitcher : MonoBehaviour
{
    [SerializeField] private TutorialType _currentTutorialType;

    public GameObject ControllersTutorial;
    public GameObject CustomControllersTutorial;

    private void Start()
    {
        switch (_currentTutorialType)
        {
            case TutorialType.Controllers:
                ControllersTutorial.gameObject.SetActive(true);
                CustomControllersTutorial.gameObject.SetActive(false);
                break;
            case TutorialType.CustomControllers:
                ControllersTutorial.gameObject.SetActive(false);
                CustomControllersTutorial.gameObject.SetActive(true);
                break;
        }
    }
}
