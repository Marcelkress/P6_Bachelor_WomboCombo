using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
public class Enemy : MonoBehaviour
{

    private int[] comboArray = new int[] { 1, 2, 3, 1, 2, 1 }; // a combo would be 1 and 2, then 3 and 1 would be another combo. 
    [SerializeField] private int comboStep = 0; // keeps track of the current step in the combo sequence
    private NavMeshAgent agent;

    private GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)
    [SerializeField] private Transform content;

    private Image SquareImage, CircleImage, TriangleImage; // references to the UI images for each button (Square, Circle, Triangle)
    public Transform player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        InitializeUI();
        // Set the destination to the player's position
        agent.SetDestination(player.position);
    }

    private void PlayerOneUpdate()
    {
        comboStep += 2;
        // Epic Effects
        UpdateUI();

    }

    private void InitializeUI()
    {
        for (int i = 0; i < comboArray.Length; i++)
        {
            // Instantiate a new UI image for each combo step and set its parent to the content transform
            GameObject uiImage = Instantiate(inputUIImage, content);

            // 1 = Square, 2 = Circle, 3 = Triangle (you can customize this mapping as needed)

                switch (comboArray[i])
                {
                    case 1:
                        uiImage.GetComponent<Image>().sprite = SquareImage.sprite; // Set the sprite to the Square image
                        break;
                    case 2:
                        uiImage.GetComponent<Image>().sprite = CircleImage.sprite; // Set the sprite to the Circle image
                        break;
                    case 3:
                        uiImage.GetComponent<Image>().sprite = TriangleImage.sprite; // Set the sprite to the Triangle image
                        break;

                }


        }
    }

    private void UpdateUI()
    {
        
    }


}
