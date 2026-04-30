using UnityEngine;
using DG.Tweening;

public class CastleDoorTrigger : MonoBehaviour
{
    [SerializeField] private Transform doorTransformPivot;
    [SerializeField] private float speed = 1f;
    [SerializeField] private Vector3 openRotation = new Vector3(0, 90, 0);
    public CanvasGroup EndOfGameCanvasGroup;
    public bool endGameWhenTriggered = false;
    [SerializeField] private AK.Wwise.Event doorSound;
    void Start()
    {

        EndOfGameCanvasGroup.alpha = 0;
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (endGameWhenTriggered && other.CompareTag("Player"))
        {
            EndOfGameCanvasGroup.DOFade(1, 2f).OnComplete(() => Time.timeScale = 0);
            return;
        }
        if (other.CompareTag("Player") && !endGameWhenTriggered)
        {
            Invoke(nameof(OpenDoor), 0.6f);
        }
    }
    private void OpenDoor()
    {

        doorTransformPivot.DORotate(openRotation, speed);
        doorSound.Post(gameObject);
    }
}
