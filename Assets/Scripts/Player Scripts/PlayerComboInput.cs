using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
[Serializable]public struct Interval
{ 
    public int min, max;
}  

public class PlayerComboInput : MonoBehaviour
{
    [Header("Input stuff")]
    public ControllerInputCrossPlatform controllerInput;
    public int controllerValue;

    [Header("Combinations")] public Interval moonStar;
    public Interval moonMoon, moonSun, starMoon, starStar, starSun, 
        sunMoon, sunStar, sunSun;
    
    [Header("Player")]
    public Player playerID;
    public enum Player
    {   
        Player1 = 1,
        Player2 = 2
    }
    
    private ControllerState inputData; 
    private PlayerInfoStruct playerInfoStruct;
    private PlayerInput input;
    
    [Header("Symbol values")] public int moon = 1;
    public int star = 2, sun = 3;

    private static bool playerOneTaken;
    
    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        
        if (playerOneTaken)
        {
            playerID = Player.Player2;
            gameObject.tag = "PlayerTwo";
            input.SwitchCurrentActionMap("Player two");
        }
        else
        {
            playerID = Player.Player1;
            gameObject.tag = "PlayerOne";
            playerOneTaken = true;
        }
        
        playerInfoStruct.symbOne = 1;
        playerInfoStruct.symbTwo = 1;

        controllerInput = FindFirstObjectByType<ControllerInputCrossPlatform>();
    }

    
    private void Update()
    {
        inputData = controllerInput.GetInput(playerID == Player.Player1 ? 1 : 2);

        if (inputData.connected == false)
            return;

        controllerValue = inputData.rotation;
        
        UpdateSymbolsFromController();
        
        if (inputData.pushed == true)
        {
            inputData.pushed = false;
            OnFire();
        }
    }
    
    
    private void UpdateSymbolsFromController()
    {
        if (controllerValue <= moonMoon.max && controllerValue >= moonMoon.min)
        {
            playerInfoStruct.symbOne = 1;
            playerInfoStruct.symbTwo = 1;
        }
        else if (controllerValue <= moonStar.max && controllerValue >= moonStar.min)
        {
            playerInfoStruct.symbOne = 1;
            playerInfoStruct.symbTwo = 2;
        }
        else if (controllerValue <= moonSun.max && controllerValue >= moonSun.min)
        {
            playerInfoStruct.symbOne = 1;
            playerInfoStruct.symbTwo = 3;
        }
        else if (controllerValue <= starMoon.max && controllerValue >= starMoon.min)
        {
            playerInfoStruct.symbOne = 2;
            playerInfoStruct.symbTwo = 1;
        }
        else if (controllerValue <= starStar.max && controllerValue >= starStar.min)
        {
            playerInfoStruct.symbOne = 2;
            playerInfoStruct.symbTwo = 2;
        }
        else if (controllerValue <= starSun.max && controllerValue >= starSun.min)
        {
            playerInfoStruct.symbOne = 2;
            playerInfoStruct.symbTwo = 3;
        }
        else if (controllerValue <= sunMoon.max && controllerValue >= sunMoon.min)
        {
            playerInfoStruct.symbOne = 3;
            playerInfoStruct.symbTwo = 1;
        }
        else if (controllerValue <= sunStar.max && controllerValue >= sunStar.min)
        {
            playerInfoStruct.symbOne = 3;
            playerInfoStruct.symbTwo = 2;
        }
        else if (controllerValue <= sunSun.max && controllerValue >= sunSun.min)
        {
            playerInfoStruct.symbOne = 3;
            playerInfoStruct.symbTwo = 3;
        }
        
        Debug.Log(playerInfoStruct.symbOne + "    " + playerInfoStruct.symbTwo);
    }

    public PlayerInfoStruct GetSymbolUpdate()
    {
        return playerInfoStruct;
    }
    
    public void OnFire()
    {
        InputManager.instance.UpdatePlayerInfo((int)playerID, playerInfoStruct);
    }

    private bool topCanChange = true, bottomCanChange = true;

    public void OnTopCycle(InputValue value)
    {
        var val = value.Get<Vector2>();

        if ((val.x < -.5 || val.x > .5) && topCanChange)
        {
            topCanChange = false;
            playerInfoStruct.symbOne += (int)val.x;

            if (playerID == Player.Player1)
            {
                if (playerInfoStruct.symbOne > 3)
                {
                    playerInfoStruct.symbOne = 2;
                }
                else if (playerInfoStruct.symbOne < 2)
                {
                    playerInfoStruct.symbOne = 3;
                }
            }
            else if (playerID == Player.Player2)
            {
                if (playerInfoStruct.symbOne > 2)
                {
                    playerInfoStruct.symbOne = 1;
                }
                else if (playerInfoStruct.symbOne < 1)
                {
                    playerInfoStruct.symbOne = 2;
                }
            }
        }
        else if (val.x == 0)
        {
            topCanChange = true;
        }
    }

    public void OnBottomCycle(InputValue value)
    {
        var val = value.Get<Vector2>();

        if ((val.x < -.5 || val.x > .5) && bottomCanChange)
        {
            bottomCanChange = false;
            playerInfoStruct.symbTwo += (int)val.x;
            
            if (playerInfoStruct.symbTwo > 3)
            {
                playerInfoStruct.symbTwo = 1;
            }
            else if (playerInfoStruct.symbTwo < 1)
            {
                playerInfoStruct.symbTwo = 3;
            }
        }
        else if (val.x == 0)
        {
            bottomCanChange = true;
        }
    }
}

[Serializable]
public struct ControllerState
{
    public int rotation;
    public bool pushed;
    public bool connected;
}
