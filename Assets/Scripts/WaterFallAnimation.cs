using UnityEngine;

public class WaterFallAnimation : MonoBehaviour
{
    public Material material;
    public float scrollSpeed = 0.5f;
    
    private void Update()
    {

        float offset = Time.time * scrollSpeed;
        material.mainTextureOffset = new Vector2(0, offset);
    }
}
