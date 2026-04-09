using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
        public void MoveTo(PathDestinationObject destination)
        {
            activeDestination = destination;
            lookSettleStarted = false;

            // Build waypoint positions: current pos → subDestinations → main destination
            List<Vector3> waypoints = new List<Vector3>();
            List<PathDestinationObject> waypointObjects = new List<PathDestinationObject>();

            waypoints.Add(transform.position);
            waypointObjects.Add(null); // no destination object for start position

            if (destination.subDestinations != null)
            {
                foreach (var sub in destination.subDestinations)
                {
                    waypoints.Add(sub.transform.position);
                    waypointObjects.Add(sub);
                }
            }

            waypoints.Add(destination.transform.position);
            waypointObjects.Add(destination);

            // Convert all to local space (BezierPath works in pathCreator local space)
            Vector3[] localPoints = new Vector3[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++)
                localPoints[i] = pathCreator.transform.InverseTransformPoint(waypoints[i]);

            // Constructor uses Automatic mode → smooth handles for all intermediate anchors
            BezierPath bezierPath = new BezierPath(localPoints, false, PathSpace.xyz);
            // Switch to Free so we can override specific handles without auto-recalculation
            bezierPath.ControlPointMode = BezierPath.ControlMode.Free;

            // Override departure handle (after-handle of first anchor, index 1)
            float totalDistance = Vector3.Distance(localPoints[0], localPoints[localPoints.Length - 1]);
            Vector3 localForward = pathCreator.transform.InverseTransformDirection(transform.forward);
            bezierPath.SetPoint(FirstTravelGizmoIndex, localPoints[0] + localForward * (totalDistance * departureStrength));

            // Override before-handle for any waypoint that has a controlPoint set
            for (int i = 1; i < waypointObjects.Count; i++)
            {
                PathDestinationObject wp = waypointObjects[i];
                if (wp != null && wp.controlPoint != null)
                {
                    int beforeHandleIndex = i * 3 - 1; // before-handle of anchor i
                    Vector3 localControl = pathCreator.transform.InverseTransformPoint(wp.controlPoint.position);
                    bezierPath.SetPoint(beforeHandleIndex, localControl);
                }
            }

            pathCreator.bezierPath = bezierPath;

            startDistance = 0f;
            dist = 0f;
            targetDistance = pathCreator.path.length;
            travelTimer = 0f;
            isMoving = true;
        }

        public float GetCurrentTripDuration()
        {
            if (activeDestination == null || pathCreator == null) return 0f;
            float speed = Mathf.Max(0.01f, activeDestination.travelSpeed);
            return pathCreator.path.length / speed;
        }

        private float dist; 
        void LateUpdate()
        {
            if (!isMoving || activeDestination == null) return;
            
            float speed = Mathf.Max(0.01f, activeDestination.travelSpeed);
            float totalTripDuration = targetDistance / speed;

            travelTimer += Time.deltaTime;
            float progress = totalTripDuration > 0f ? Mathf.Clamp01(travelTimer / totalTripDuration) : 1f;

            float curved = activeDestination.moveCurve.Evaluate(progress);

            dist = Mathf.Lerp(startDistance, targetDistance, curved);
            transform.position = pathCreator.path.GetPointAtDistance(dist, endOfPathInstruction);
            if (activeDestination.rotateTowardsPath)
            {
                if (activeDestination.lookTarget != null && progress >= activeDestination.lookBlendStart)
                {
                    if (!lookSettleStarted || activeDestination.shouldSettleLook)
                    {
                        lookSettleStarted = true;
                        lookStartRotation = transform.rotation;
                    }
                    RotationBasedOnDestinationObject(progress);
                }
                else
                {
                    RotationBasedOnBezierCurve();
                }
            }

            if (cam) cam.fieldOfView = baseFov + activeDestination.fovBoost * Mathf.Sin(progress * Mathf.PI); // fov boost that peaks at the middle of the trip. Slower at the start and end.

            if (progress >= 1f)
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
