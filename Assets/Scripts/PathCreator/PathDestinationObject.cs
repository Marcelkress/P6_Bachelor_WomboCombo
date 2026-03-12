using UnityEngine;

public class PathDestinationObject : MonoBehaviour
{
    public float travelDuration = 2f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float fovBoost = 10f;
    public float arrivalShakeDuration = 0.3f;
    public float arrivalShakeMagnitude = 0.15f;

    public bool rotateTowardsPath = true;

    [Header("Curve")]
    public Transform controlPoint;

}
