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
    private PlayerComboInput comboInput;
    public InputReceiver inputReceiver;
    public int controllerValue;
    private int minVal = 0, maxVal = 127;

    public Interval SunMoon;
    
    [Header("Sun / Moon interval")] public int sunMin;
    public int sunMax;
    [Header("Moon interval")] public int moonMin;
    public int moonMax;
    [Header("Star interval")] public int starMin;
    public int starMax;

    private InputStruct inputData;
    
    [Header("Player")]
    public Player playerID;
    public enum Player
    {   
        Player1 = 1,
        Player2 = 2
    }
    
    private PlayerInfoStruct playerInfoStruct;
    private PlayerInput input;
    
    [Header("Symbol values")] public int triangle = 1;
    public int square = 2, circle = 3;

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
    }

    private void Update()
    {
        if (playerID == Player.Player1)
        {
            inputData = inputReceiver.GetInput(1);
        }
        else
        {
            inputData = inputReceiver.GetInput(2);
        }

        controllerValue = inputData.rotationVal;
        
        float val = Mathf.Clamp(controllerValue, minVal, maxVal);

        if (val < SunMoon.max && val > SunMoon.min)
        {
            Debug.Log("SUN / Moon");
            // playerInfoStruct.symbOne = 
        }
        else if (val < moonMax && val > moonMin)
        {
            Debug.Log("MOON!");
        }
        else if (val < starMax && val > starMin)
        {
            Debug.Log("STAR!");
        }


        if (inputData.pressVal == 1)
        {
            OnFire();
        }
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

        //Debug.Log("Top cycle");

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

        //Debug.Log("Bottom cycle");

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
