using UnityEngine;

public class EnemyCanvasOri : MonoBehaviour
{
    private Transform player;

    private RectTransform rectTransform; // Reference to the RectTransform component of the enemy canvas


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform; // Find the player by tag and get its transform

        rectTransform = GetComponent<RectTransform>(); // Get the RectTransform component of the enemy canvas
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 directionToPlayer = transform.position - player.position; // Calculate the direction from the enemy to the player
        directionToPlayer.y = 0; // Keep the enemy upright by ignoring the y-axis

        rectTransform.rotation = Quaternion.LookRotation(directionToPlayer); // Rotate the enemy canvas to face the player

    }
}
