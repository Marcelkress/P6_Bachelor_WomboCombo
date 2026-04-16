using UnityEngine;
using System.Collections;


public class RFX4_CameraShake : MonoBehaviour
{
    public AnimationCurve ShakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public float Duration = 2;
    public float Speed = 22;
    public float Magnitude = 1;
    public float DistanceForce = 100;
    public float RotationDamper = 2;
    public bool IsEnabled = true;

    bool isPlaying;
    [HideInInspector]
    public bool canUpdate;

    void PlayShake()
    {
        StopAllCoroutines();
        StartCoroutine(Shake());
    }

    void Update()
    {
        if (isPlaying && IsEnabled) {
            isPlaying = false;
            PlayShake();
        }
    }

    void OnEnable()
    {
        isPlaying = true;
        var shakes = FindObjectsOfType(typeof(RFX4_CameraShake)) as RFX4_CameraShake[];
        if(shakes!=null)
        foreach (var shake in shakes)
        {
            shake.canUpdate = false;
        }
        canUpdate = true;
    }

    IEnumerator Shake()
{
    var shaker = GameObject.FindGameObjectWithTag("CameraShaker");
    if (shaker == null) yield break;

    var camT = shaker.transform;
    var elapsed = 0f;
    var time = 0f;
    var randomStart = Random.Range(-1000.0f, 1000.0f);
    var distanceDamper = 1 - Mathf.Clamp01((camT.position - transform.position).magnitude / DistanceForce);
    var direction = (transform.position - camT.position).normalized;

    var baseLocalPos = camT.localPosition;
    var baseLocalRot = camT.localRotation;

    try
    {
        while (elapsed < Duration && canUpdate)
        {
            elapsed += Time.deltaTime;
            var percentComplete = elapsed / Duration;
            var damper = ShakeCurve.Evaluate(percentComplete) * distanceDamper;
            time += Time.deltaTime * damper;

            var posOffset = -direction * Time.deltaTime * Mathf.Sin(time * Speed) * damper * (Magnitude * 0.5f);

            var alpha = randomStart + Speed * percentComplete / 10f;
            var x = Mathf.PerlinNoise(alpha, 0.0f) * 2.0f - 1.0f;
            var y = Mathf.PerlinNoise(1000 + alpha, alpha + 1000) * 2.0f - 1.0f;
            var z = Mathf.PerlinNoise(0.0f, alpha) * 2.0f - 1.0f;

            var rotOffset = Mathf.Sin(time * Speed) * damper * Magnitude *
                            new Vector3(0.5f + y, 0.3f + x, 0.3f + z) * RotationDamper;

            camT.localPosition = baseLocalPos + posOffset;
            camT.localRotation = baseLocalRot * Quaternion.Euler(rotOffset);

            yield return null;
        }
    }
    finally
    {
        camT.localPosition = Vector3.zero;
        camT.localRotation = Quaternion.identity;
    }
}
}
