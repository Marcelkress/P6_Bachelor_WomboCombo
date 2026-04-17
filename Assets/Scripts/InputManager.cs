using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public struct PlayerInfoStruct
{
    public int symbOne;
    public int symbTwo;
    public bool newData;
}
public class InputManager : MonoBehaviour
{
    public static InputManager instance;
    public bool debug = false;
    
    private PlayerInfoStruct playerOneCurrent, playerTwoCurrent;
    private readonly Queue<PlayerInfoStruct> playerOnePending = new Queue<PlayerInfoStruct>();
    private readonly Queue<PlayerInfoStruct> playerTwoPending = new Queue<PlayerInfoStruct>();

    public UnityEvent PlayerOneEvent, PlayerTwoEvent;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Update()
    {
        while (playerOnePending.Count > 0)
        {
            var next = playerOnePending.Dequeue();
            playerOneCurrent.symbOne = next.symbOne;
            playerOneCurrent.symbTwo = next.symbTwo;
            playerOneCurrent.newData = true;

            PlayerOneEvent.Invoke();
            playerOneCurrent.newData = false;

            if (debug)
            {
                Debug.Log("new data from p1:");
                Debug.Log("Top Symbol: " + playerOneCurrent.symbOne.ToString());
                Debug.Log("Bottom Symbol: " + playerOneCurrent.symbTwo.ToString());
            }
        }

        while (playerTwoPending.Count > 0)
        {
            var next = playerTwoPending.Dequeue();
            playerTwoCurrent.symbOne = next.symbOne;
            playerTwoCurrent.symbTwo = next.symbTwo;
            playerTwoCurrent.newData = true;

            PlayerTwoEvent.Invoke();
            playerTwoCurrent.newData = false;

            if (debug)
            {
                Debug.Log("new data from p2");
                Debug.Log("Top Symbol: " + playerTwoCurrent.symbOne.ToString());
                Debug.Log("Bottom Symbol: " + playerTwoCurrent.symbTwo.ToString());
            }
        }
        
        // Update combo
    }

    public PlayerInfoStruct GetPlayerSymbols(int id)
    {
        return id == 1 ? playerOneCurrent : playerTwoCurrent;
    }
    
    public void UpdatePlayerInfo(int id, PlayerInfoStruct playerInfoStruct)
    {
        if (id == 1)
        {
            playerOnePending.Enqueue(playerInfoStruct);
        }

        if (id == 2)
        {
            playerTwoPending.Enqueue(playerInfoStruct);
        }
    }

    private PlayerInputManager playerInputManager;
    
    public void JoinPlayer()
    {
        playerInputManager = GetComponent<PlayerInputManager>();

        if (playerInputManager.playerCount < 2)
        {
            playerInputManager.JoinPlayer();
        }
    }
}
