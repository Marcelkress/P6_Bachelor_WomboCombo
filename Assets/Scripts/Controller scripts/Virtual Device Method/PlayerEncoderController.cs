// PlayerEncoderController.cs
using UnityEngine;

public class PlayerEncoderController : MonoBehaviour
{
    [Header("Rotation")]
    public Transform bottomHemisphere;
    public bool  smoothRotation = true;
    public float smoothSpeed    = 8f;

    float targetAngle;
    float currentAngle;

    void Update()
    {
        if (bottomHemisphere == null) return;

        currentAngle = smoothRotation
            ? Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothSpeed)
            : targetAngle;

        bottomHemisphere.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }

    public void OnEncoderData(int position, bool buttonPressed)
    {
        targetAngle = (position / 127f) * 360f;

        if (buttonPressed)
            Debug.Log("Button pushed");
    }
}