using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Image top, bottom;

    public Sprite circle, square, triangle;

    public PlayerComboInput playerInput;

    public bool enable;
    
    void Start()
    {
        if (enable == false)
        {
            top.gameObject.SetActive(false);
            bottom.gameObject.SetActive(false);
        }
    }

    public void PlayerOneJoined()
    {
        StartCoroutine(Delay(1));
    }

    public void PlayerTwoJoined()
    {
        StartCoroutine(Delay(2));
    }

    private IEnumerator Delay(int id)
    {
        yield return new WaitForSeconds(0.1f);
        
        if(GameObject.FindGameObjectWithTag("PlayerOne") && id == 1)
            playerInput = GameObject.FindGameObjectWithTag("PlayerOne").GetComponent<PlayerComboInput>();
        
        if(GameObject.FindGameObjectWithTag("PlayerTwo") && id == 2)
            playerInput = GameObject.FindGameObjectWithTag("PlayerTwo").GetComponent<PlayerComboInput>();
    }
    
    private void Update()
    {
        if (!enable || playerInput == null)
        {
            return;
        }
        
        PlayerInfoStruct info = playerInput.GetSymbolUpdate();

        switch (info.symbOne)
        {
            case 1: 
                top.GetComponent<Image>().sprite = triangle; // Set the sprite to the Square image
                break;
            case 2:
                top.GetComponent<Image>().sprite = square; // Set the sprite     to the Circle image
                break;
            case 3:
                top.GetComponent<Image>().sprite = circle; // Set the sprite to the Triangle image
                break;
        }
        
        switch (info.symbTwo)
        {
            case 1: 
                bottom.GetComponent<Image>().sprite = triangle; // Set the sprite to the Square image
                break;
            case 2:
                bottom.GetComponent<Image>().sprite = square; // Set the sprite     to the Circle image
                break;
            case 3:
                bottom.GetComponent<Image>().sprite = circle; // Set the sprite to the Triangle image
                break;
        }
    }

}
