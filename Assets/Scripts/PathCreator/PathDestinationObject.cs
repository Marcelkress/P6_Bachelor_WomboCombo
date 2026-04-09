using UnityEngine;

public class PathDestinationObject : MonoBehaviour
{
    [Tooltip("Constant speed in world units per second")]
    public float travelSpeed = 10f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float fovBoost = 10f;
    public float arrivalShakeDuration = 0.3f;
    public float arrivalShakeMagnitude = 0.15f;

    public bool rotateTowardsPath = true;
    public bool overwriteSubDestinations = false; 
    public PathDestinationObject[] subDestinations; // For more control between main destination, will move between these in order to reach the main destination

    [Header("Curve")]
    public Transform controlPoint;

    [Header("Arrival Look")]
    public bool shouldSettleLook = true; // Whether to start the look settle timer once the blend starts, or to wait until the blend is fully done
    public Transform lookTarget;
    public float lookSettleDuration = 0.5f;
    [Range(0f, 1f)]
    public float lookBlendStart = 0.7f;

    private void Start()
    {
        if (!overwriteSubDestinations) return;
        
        for (int i = 0; i < subDestinations.Length; i++)
        {
            if (subDestinations[i] == null)
            {
                subDestinations[i].shouldSettleLook = false;
                subDestinations[i].travelSpeed = this.travelSpeed;
                subDestinations[i].moveCurve = this.moveCurve;
                subDestinations[i].fovBoost = this.fovBoost;
            }
        }
    }
    // Gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);
        if (controlPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, controlPoint.position);
            if (subDestinations != null)
            {
                foreach (var sub in subDestinations)
                {
                    Gizmos.DrawLine(controlPoint.position, sub.transform.position);
                }
            }
        }
        if (lookTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, lookTarget.position);
        }
    }

}
