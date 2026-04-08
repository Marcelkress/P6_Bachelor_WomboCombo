using UnityEngine;

public class EnemyCanvasOri : MonoBehaviour
{
    private Transform MainCamera; // Reference to the main camera

    private RectTransform rectTransform; // Reference to the RectTransform component of the enemy canvas
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MainCamera = Camera.main.transform; // Get the main camera's transform
        rectTransform = GetComponent<RectTransform>(); // Get the RectTransform component of the enemy canvas
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 directionToPlayer = transform.position - MainCamera.position; // Calculate the direction from the enemy to the player
        directionToPlayer.y = 0; // Keep the enemy upright by ignoring the y-axis

        rectTransform.rotation = Quaternion.LookRotation(directionToPlayer); // Rotate the enemy canvas to face the player
    }
}
