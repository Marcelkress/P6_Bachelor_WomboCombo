using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
public class Enemy : MonoBehaviour
{
   
    private int[] comboArray = new int[] {  1, 3, 2, 1 ,3, 1 }; // a combo would be 1 and 2, then 3 and 1 would be another combo. 
    [SerializeField] private int comboStep = 0; // keeps track of the current step in the combo sequence
    private NavMeshAgent agent;

    public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)
    [SerializeField] private Transform content;
    private Image[] contentSprite;

    public Sprite SquareImage, CircleImage, TriangleImage; // references to the UI images for each button (Square, Circle, Triangle)
    public Transform player;

    private GridLayoutGroup gridLayoutGroup; // reference to the GridLayoutGroup component

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        player = GameObject.FindGameObjectWithTag("Player").transform; // Find the player by tag and get its transform

        InitializeUI();
        // Set the destination to the player's position
        agent.SetDestination(player.position);
    }
 
    public void PlayerOneUpdate()
    {
        
        // Epic Effects
        UpdateUI();
        comboStep ++;

    }

    private void InitializeUI()
    {
        contentSprite = new Image[comboArray.Length]; // Initialize the contentSprite array to match the length of comboArray

        for (int i = 0; i < comboArray.Length; i++)
        {
            // Instantiate a new UI image for each combo step and set its parent to the content transform
            GameObject uiImage = Instantiate(inputUIImage, content);

            // 1 = Square, 2 = Circle, 3 = Triangle (you can customize this mapping as needed)

                switch (comboArray[i])
                {
                    case 1:
                        uiImage.GetComponent<Image>().sprite = SquareImage; // Set the sprite to the Square image
                        break;
                    case 2:
                        uiImage.GetComponent<Image>().sprite = CircleImage; // Set the sprite     to the Circle image
                        break;
                    case 3:
                        uiImage.GetComponent<Image>().sprite = TriangleImage; // Set the sprite to the Triangle image
                        break;

                }

            contentSprite[i] = uiImage.GetComponent<Image>(); // Store the Image component in the contentSprite array
        }

        
    }

    private void UpdateUI()
    {

        contentSprite[(comboStep * 2) + 1].enabled = false;
        contentSprite[(comboStep * 2) ].enabled = false;
        
    }


}
