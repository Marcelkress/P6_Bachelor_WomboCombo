using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public int health = 3; // Example health value for the player
    private int currentHealth;  

    public Image healthBar; // Reference to the health bar UI element

    private int[] healingComboArray = new int[] {  1, 3, 1, 2 ,1, 1 };
    private int comboStep = 0; // keeps track of the current step in the combo sequence

    public Sprite SquareImage, CircleImage, TriangleImage;

    public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)

    public GameObject gameOverScreen; // Reference to the Game Over screen UI element
    [SerializeField] private Transform content;
    private Image[] contentSprite;

    private PlayerInput playerInput; // Reference to the PlayerInput component for handling input actions

    void Start()
    {
        currentHealth = health; // Initialize current health to the maximum health at the start

        playerInput = GetComponent<PlayerInput>(); // Get the PlayerInput component attached to the player game object


        InitializeUI();
    }

    void Update()
    {
        // Example of checking for input to trigger the healing spell (you can customize this as needed)
        if (playerInput.actions["Heal"].triggered) // Assuming you have an input action named "Heal"
        {
            if (currentHealth == health) // Only heal if the player's current health is below the maximum health
            {
                return; // Exit the method without healing if the player's health is already full
            }
            HealingSpell(); // Call the HealingSpell method when the input action is triggered
        }
    }

    private void InitializeUI()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)currentHealth / health; // Set the initial fill amount of the health bar
        }

        contentSprite = new Image[healingComboArray.Length]; // Initialize the contentSprite array to match the length of healingComboArray

        for (int i = 0; i < healingComboArray.Length; i++)
        {
            // Instantiate a new UI image for each combo step and set its parent to the content transform
            GameObject uiImage = Instantiate(inputUIImage, content);

            // 1 = Square, 2 = Circle, 3 = Triangle (you can customize this mapping as needed)

                switch (healingComboArray[i])
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
        private int rightIndex = 0;
        private int leftIndex = 0;
    private void HealingSpell()
    {

        rightIndex = comboStep * 2 + 1;
        leftIndex = comboStep * 2;

        contentSprite[leftIndex].enabled = false;
        contentSprite[rightIndex].enabled = false;

        comboStep++; // Move to the next step in the combo sequence

        // if fully complete heal player for 1 health and reset combo
        if (comboStep * 2 >= healingComboArray.Length)
        {
            
            currentHealth += 1; // Heal the player for
            Debug.Log("Player healed! Current health: " + currentHealth);

            // Update the health bar UI element
            if (healthBar != null)
            {
                healthBar.fillAmount = (float)currentHealth / health; // Update the fill amount
                AnimateHealthBar(); // Optional: Add a tweening effect to the health bar for smoother transitions
                rightIndex = 0; // Reset the right index for the next combo
                leftIndex = 0; // Reset the left index for the next combo
                comboStep = 0; // Reset the combo step for the next combo

                foreach (Image img in contentSprite)
                {
                    img.enabled = true; // Re-enable all combo step UI elements for the next combo
                }

            }
        }


    }

    


    public void TakeDamage(int damage)
    {
        currentHealth -= damage; // Reduce the player's health by the damage amount
        Debug.Log("Player took damage! Current health: " + currentHealth);

        // Update the health bar UI element
        if (healthBar != null)
        {

            if (currentHealth < health/2)
            {
                healthBar.fillAmount = (float)currentHealth / health;
                 // Optional: Add a tweening effect to the health bar for smoother transitions
                AnimateHealthBar();
                ShakeHealthBar();

            }
            else
            {
                healthBar.fillAmount = (float)currentHealth / health;
            // Optional: Add a tweening effect to the health bar for smoother transitions
            AnimateHealthBar();
            }

            

            
        }

        if (currentHealth <= 0)
        {
            Die(); // Call the Die method if health drops to 0 or below
        }

    }

    private void AnimateHealthBar()
    {
        // Example of using DOTween to animate the health bar fill amount
        if (healthBar != null)
        {
            healthBar.DOFillAmount((float)currentHealth / health, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    private void ShakeHealthBar()
    {
        if (healthBar != null)
        {
            // Example of using DOTween to shake the health bar
            healthBar.transform.DOShakePosition(0.5f, new Vector3(10f, 0f, 0f), 10, 90, false);
        }
    }

    private void Die()
    {
        gameOverScreen.SetActive(true); // Show the Game Over screen
        Time.timeScale = 0f; // Pause the game by setting time scale to
    }

}
