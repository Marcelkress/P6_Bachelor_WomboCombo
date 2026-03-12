using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerComboInput : MonoBehaviour
{
    public enum Player
    {   
        Player1 = 1,
        Player2 = 2
    }
    
    public Player playerID;
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
