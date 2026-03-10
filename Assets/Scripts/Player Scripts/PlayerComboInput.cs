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
    private PlayerInput input;
    
    [Header("Symbol values")] public int triangle = 1;
    public int square = 2, circle = 3;

    private static bool taken;
    
    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        if (taken)
        {
            playerID = Player.Player2;
            gameObject.tag = "PlayerTwo";
            input.SwitchCurrentActionMap("Player two");
            //Debug.Log(GetComponent<PlayerInput>().currentActionMap.name);
        }
        else
        {
            playerID = Player.Player1;
            gameObject.tag = "PlayerOne";
            taken = true;
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
    
    public void OnTopCircle()
    {
        playerInfoStruct.symbOne = circle;
    }

    public void OnTopSquare()
    {
        
            playerInfoStruct.symbOne = square;
        
    }

    public void OnTopTriangle()
    {
        
            playerInfoStruct.symbOne = triangle;
        
    }

    public void OnBottomCircle()
    {
        
            playerInfoStruct.symbTwo = circle;
        
    }

    public void OnBottomSquare()
    {
        
            playerInfoStruct.symbTwo = square;
        
    }

    public void OnBottomTriangle()
    {
        
            playerInfoStruct.symbTwo = triangle;
        
    }
}
