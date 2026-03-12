using UnityEngine;
using System.Collections;

namespace PathCreation.Examples
{
    public class PathFollower : MonoBehaviour
    {
        public PathCreator pathCreator;
        public EndOfPathInstruction endOfPathInstruction;

        [Header("Destinations")]
        public PathDestinationObject[] destinations;

        [Header("Departure")]
        [Range(0f, 1f)]
        public float departureStrength = 0.3f;

        private PathDestinationObject activeDestination;
        private bool isMoving;
        private float travelTimer;
        private float startDistance;
        private float targetDistance;
        private Camera cam;
        private float baseFov;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam) baseFov = cam.fieldOfView;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);
            MoveTo(0);

            yield return new WaitForSeconds(destinations[0].travelDuration + 1f);
            MoveTo(1);
        }

        public void MoveTo(int index)
        {
            if (index < 0 || index >= destinations.Length) return;
            MoveTo(destinations[index]);
        }

        private int firstBlueGizmoIndex = 1;
        private int secondBlueGizmoIndex = 2;
        public void MoveTo(PathDestinationObject destination)
        {
            activeDestination = destination;

            // BezierPath works in pathCreator local space
            // sørger for at konvertere destination og start position til local space, så det ikke giver nogle vilde curves, da det er en retning der er vigtig for kameraet at følge.
            Vector3 localStart = pathCreator.transform.InverseTransformPoint(transform.position);
            Vector3 localEnd = pathCreator.transform.InverseTransformPoint(destination.transform.position);
            
            BezierPath bezierPath = new BezierPath(new Vector3[] { localStart, localEnd }, false, PathSpace.xyz); // faktisk laver pathen
            bezierPath.ControlPointMode = BezierPath.ControlMode.Free;

            // Automatic control mode generates unpredictable handles with only 2 points.
            // Manually place control points at 1/3 and 2/3 for a clean straight path.
            // Så de ikke bliver placeret tilfældigt, da det er en retning der er vigtig for kameraet at følge. Ellers kan det give nogle ret vilde curves. (De blå gizmos i editoren)
            // Start handle follows the follower's current forward direction,
            // scaled by departureStrength * path length so it's proportional to the distance travelled
            float pathLength = Vector3.Distance(localStart, localEnd);
            Vector3 localForward = pathCreator.transform.InverseTransformDirection(transform.forward);
            bezierPath.SetPoint(firstBlueGizmoIndex, localStart + localForward * (pathLength * departureStrength));

            // End handle is pulled towards the destination's control point if set, otherwise straight
            if (destination.controlPoint != null)
            {
                Vector3 localControl = pathCreator.transform.InverseTransformPoint(destination.controlPoint.position);
                bezierPath.SetPoint(secondBlueGizmoIndex, localControl);
            }
            else
            {
                bezierPath.SetPoint(secondBlueGizmoIndex, Vector3.Lerp(localStart, localEnd, 2f / 3f)); // får bare default value
            }
           

            pathCreator.bezierPath = bezierPath; // assign pathen til pathCreator, så den kan bruges i LateUpdate til at flytte kameraet

            startDistance = 0f;
            targetDistance = pathCreator.path.length;
            travelTimer = 0f;
            isMoving = true;
        }

        void LateUpdate()
        {
            if (!isMoving || activeDestination == null) return;

            travelTimer += Time.deltaTime;
            float t = Mathf.Clamp01(travelTimer / activeDestination.travelDuration);
            float curved = activeDestination.moveCurve.Evaluate(t);

            float dist = Mathf.Lerp(startDistance, targetDistance, curved);
            transform.position = pathCreator.path.GetPointAtDistance(dist, endOfPathInstruction);
            if (activeDestination.rotateTowardsPath)
            {
                Vector3 pathDirection = pathCreator.path.GetDirectionAtDistance(dist, endOfPathInstruction);
                float targetYAngle = Mathf.Atan2(pathDirection.x, pathDirection.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, targetYAngle, 0f);
            }

            if (cam) cam.fieldOfView = baseFov + activeDestination.fovBoost * Mathf.Sin(t * Mathf.PI);

            if (t >= 1f)
            {
                isMoving = false;
                if (cam) cam.fieldOfView = baseFov;
                StartCoroutine(ArrivalShake());
            }
        }

        IEnumerator ArrivalShake()
        {
            Vector3 origin = transform.position;
            float elapsed = 0f;
            while (elapsed < activeDestination.arrivalShakeDuration)
            {
                float strength = activeDestination.arrivalShakeMagnitude * (1f - elapsed / activeDestination.arrivalShakeDuration);
                transform.position = origin + Random.insideUnitSphere * strength;
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = origin;
        }
    }
}
