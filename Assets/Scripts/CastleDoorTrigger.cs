using UnityEngine;
using DG.Tweening;

public class CastleDoorTrigger : MonoBehaviour
{
    [SerializeField] private Transform doorTransformPivot;
    [SerializeField] private float speed = 1f;
    [SerializeField] private Vector3 openRotation = new Vector3(0, 90, 0);
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Invoke(nameof(OpenDoor), 0.6f);
        }
    }
    private void OpenDoor()
    {

        doorTransformPivot.DORotate(openRotation, speed);
    }
}
