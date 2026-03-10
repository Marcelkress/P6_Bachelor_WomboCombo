using System;
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

    [Header("Symbol values")] public int triangle = 1;
    public int square = 2, circle = 3;
    
    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            InputManager.instance.UpdatePlayerInfo((int)playerID, playerInfoStruct);
        }
    }

    public void OnTopCircle(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            playerInfoStruct.symbOne = circle;
        }
    }

    public void OnTopSquare(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            playerInfoStruct.symbOne = square;
        }
    }

    public void OnTopTriangle(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            playerInfoStruct.symbOne = triangle;
        }
    }

    public void OnBottomCircle(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            playerInfoStruct.symbTwo = circle;
        }
    }

    public void OnBottomSquare(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            playerInfoStruct.symbTwo = square;
        }
    }

    public void OnBottomTriangle(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            playerInfoStruct.symbTwo = triangle;
        }
    }
}
