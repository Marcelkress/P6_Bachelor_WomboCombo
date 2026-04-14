using UnityEngine;

[CreateAssetMenu(fileName = "UniCanvasSizerSettings", menuName = "Scriptable Objects/Universal Canvas Sizer Settings")]
public class UniCanvasSizerSettings : ScriptableObject
{
    public int reducingFactor = 2; // Factor by which the canvas size is reduced when the player is close
    public float startScalingDistance = 30f;
    public float maxFarScaleMultiplier = 1.5f; // Maximum scale multiplier when the canvas is far from the player
    public float farScaleDistance = 90f; // Distance at which the canvas reaches its maximum size when far from the player
    
}
