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

        [Header("Rotation")]
        public float rotationSpeed = 5f;

        private PathDestinationObject activeDestination;
        private bool isMoving;
        private bool lookSettleStarted;
        private Quaternion lookStartRotation;
        private float travelTimer;
        private float startDistance;
        private float targetDistance;
        private Camera cam;
        private float baseFov;

        void Awake()
        {
            cam = GetComponentInChildren<Camera>();
            if (cam) baseFov = cam.fieldOfView;
        }

        public void MoveTo(int index)
        {
            if (index < 0 || index >= destinations.Length) return;
            MoveTo(destinations[index]);
        }

        private int FirstTravelGizmoIndex = 1;
        private int SecondTravelGizmoIndex = 2;
        public void MoveTo(PathDestinationObject destination)
        {
            activeDestination = destination;
            lookSettleStarted = false;

            // BezierPath works in pathCreator local space
            // sørger for at konvertere destination og start position til local space, så det ikke giver nogle vilde curves, da det er en retning der er vigtig for kameraet at følge.
            Vector3 localStart = pathCreator.transform.InverseTransformPoint(transform.position);
            Vector3 localEnd = pathCreator.transform.InverseTransformPoint(destination.transform.position);
            
            BezierPath bezierPath = new BezierPath(new Vector3[] { localStart, localEnd }, false, PathSpace.xyz); // faktisk laver pathen
            bezierPath.ControlPointMode = BezierPath.ControlMode.Free;

            // Automatic control mode generates unpredictable handles with only 2 points.
            // Så de ikke bliver placeret tilfældigt, da det er en retning der er vigtig for kameraet at følge. Ellers kan det give nogle ret vilde curves. (De blå gizmos i editoren)
            float pathLength = Vector3.Distance(localStart, localEnd);
            Vector3 localForward = pathCreator.transform.InverseTransformDirection(transform.forward);
            bezierPath.SetPoint(FirstTravelGizmoIndex, localStart + localForward * (pathLength * departureStrength)); // den blå gizmo bliver placeret i dens nuværende fremadrettede retning, den blå gizmo er scalet pathlength. Men bliver ikke rigtig brugt

            // End handle is pulled towards the destination's control point if set, otherwise straight
            if (destination.controlPoint != null)
            {
                Vector3 localControl = pathCreator.transform.InverseTransformPoint(destination.controlPoint.position); // laver til local space 
                bezierPath.SetPoint(SecondTravelGizmoIndex, localControl); // den blå gizmo bliver placeret i destinationens control point, hvis den er sat
            }
            else
            {
                bezierPath.SetPoint(SecondTravelGizmoIndex, Vector3.Lerp(localStart, localEnd, 2f / 3f)); // får bare default value
            }
           

            pathCreator.bezierPath = bezierPath; // assign pathen til pathCreator, så den kan bruges i LateUpdate til at flytte kameraet

            startDistance = 0f;
            targetDistance = pathCreator.path.length;
            travelTimer = 0f;
            isMoving = true;
        }

        private float dist; 
        void LateUpdate()
        {
            if (!isMoving || activeDestination == null) return;

            travelTimer += Time.deltaTime;
            float time = Mathf.Clamp01(travelTimer / activeDestination.travelDuration); // clamp tiden mellem 0 og 1, så den ikke overskrider destinationens travelDuration
            float curved = activeDestination.moveCurve.Evaluate(time); // bruger destinationens moveCurve til at få en kurvet tid, så bevægelsen ikke er lineær

            dist = Mathf.Lerp(startDistance, targetDistance, curved);
            transform.position = pathCreator.path.GetPointAtDistance(dist, endOfPathInstruction);
            if (activeDestination.rotateTowardsPath)
            {
                if (activeDestination.lookTarget != null && time >= activeDestination.lookBlendStart)
                {
                    if (!lookSettleStarted)
                    {
                        lookSettleStarted = true;
                        lookStartRotation = transform.rotation;
                    }
                    RotationBasedOnDestinationObject(time);
                }
                else
                {
                    RotationBasedOnBezierCurve();
                }
            }

            if (cam) cam.fieldOfView = baseFov + activeDestination.fovBoost * Mathf.Sin(time * Mathf.PI); // fov boost that peaks at the middle of the trip. Slower at the start and end.

            if (time >= 1f)
            {   
                isMoving = false;
                if (cam) cam.fieldOfView = baseFov;
                StartCoroutine(ArrivalShake());
            }
        }

        private void RotationBasedOnDestinationObject(float currentTime)
        {
            Vector3 directionToLook = activeDestination.lookTarget.position - transform.position;
            directionToLook.y = 0f; // ignorer forskel for rotation

            Quaternion endRotation = directionToLook != Vector3.zero ? Quaternion.LookRotation(directionToLook) : lookStartRotation;

            // Calculate progress strictly based on the remaining path time
            float blendRange = 1f - activeDestination.lookBlendStart;
            float time = blendRange > 0f ? Mathf.Clamp01((currentTime - activeDestination.lookBlendStart) / blendRange) : 1f;

            transform.rotation = Quaternion.Slerp(lookStartRotation, endRotation, time);
              
        }

        private void RotationBasedOnBezierCurve()
        {
            Vector3 pathDirection = pathCreator.path.GetDirectionAtDistance(dist, endOfPathInstruction);
            pathDirection.y = 0f; // ingorer forskel for rotation
            if (pathDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(pathDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

        }

        private IEnumerator ArrivalShake()
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
