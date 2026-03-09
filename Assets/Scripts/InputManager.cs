using System;
using UnityEngine;

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
    private bool newInfoPOne, newInfoPTwo;

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
        if (playerOneCurrent.newData)
        {
            playerOneCurrent.newData = false;

            if (debug)
            {
                Debug.Log("new data from p1:");
                Debug.Log("Top Symbol: " + playerOneCurrent.symbOne.ToString());
                Debug.Log("Bottom Symbol: " + playerOneCurrent.symbTwo.ToString());
            }
        }

        if (playerTwoCurrent.newData)
        {
            playerTwoCurrent.newData = false;

            if (debug)
            {
                Debug.Log("new data from p2");
                Debug.Log("Top Symbol: " + playerTwoCurrent.symbOne.ToString());
                Debug.Log("Bottom Symbol: " + playerTwoCurrent.symbTwo.ToString());
            }
        }
    }

    public void UpdatePlayerInfo(int id, PlayerInfoStruct playerInfoStruct)
    {
        if (id == 1)
        {
            playerOneCurrent.newData = true;
            playerOneCurrent.symbOne = playerInfoStruct.symbOne;
            playerOneCurrent.symbTwo = playerInfoStruct.symbTwo;
        }

        if (id == 2)
        {
            playerTwoCurrent.newData = true;
            playerTwoCurrent.symbOne = playerInfoStruct.symbOne;
            playerTwoCurrent.symbTwo = playerInfoStruct.symbTwo;
        }
    }
}
