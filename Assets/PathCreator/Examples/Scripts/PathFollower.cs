using UnityEngine;
using System.Collections;

namespace PathCreation.Examples
{
    public class PathFollower : MonoBehaviour
    {
        public PathCreator pathCreator;
        public EndOfPathInstruction endOfPathInstruction;
        public Transform[] transforms;

        [Header("Movement")]
        public float travelDuration = 2f;
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Epic")]
        public float fovBoost = 10f;
        public float arrivalShakeDuration = 0.3f;
        public float arrivalShakeMagnitude = 0.15f;

        int indexToMoveTo = -1;
        bool isMoving;
        float travelTimer;
        float startDistance;
        float targetDistance;
        Camera cam;
        float baseFov;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam) baseFov = cam.fieldOfView;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);
            ProgressPlayer();
            yield return new WaitForSeconds(10f);
            ProgressPlayer();
        }

        void LateUpdate()
        {
            if (!isMoving) return;

            travelTimer += Time.deltaTime;
            float t = Mathf.Clamp01(travelTimer / travelDuration);
            float curved = moveCurve.Evaluate(t);

            float dist = Mathf.Lerp(startDistance, targetDistance, curved);
            transform.position = pathCreator.path.GetPointAtDistance(dist, endOfPathInstruction);
            // FOV swells at peak speed (midpoint), then returns to normal
            if (cam) cam.fieldOfView = baseFov + fovBoost * Mathf.Sin(t * Mathf.PI);

            if (t >= 1f)
            {
                isMoving = false;
                if (cam) cam.fieldOfView = baseFov;
                StartCoroutine(ArrivalShake());
            }
        }

        public void ProgressPlayer()
        {
            indexToMoveTo++;
            if (indexToMoveTo >= transforms.Length) return;

            startDistance = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
            targetDistance = pathCreator.path.GetClosestDistanceAlongPath(transforms[indexToMoveTo].position);
            travelTimer = 0f;
            isMoving = true;
        }

        IEnumerator ArrivalShake()
        {
            Vector3 origin = transform.position;
            float elapsed = 0f;
            while (elapsed < arrivalShakeDuration)
            {
                float strength = arrivalShakeMagnitude * (1f - elapsed / arrivalShakeDuration);
                transform.position = origin + Random.insideUnitSphere * strength;
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = origin;
        }
    }
}
