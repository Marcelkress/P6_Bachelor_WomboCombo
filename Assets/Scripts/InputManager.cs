using UnityEngine;

public struct PlayerInfo
{
    public int symbOne;
    public int symbTwo;
    public bool newData;
}

public class InputManager : MonoBehaviour
{
    public PlayerInfo playerOneCurrent, playerTwoCurrent;
    private bool newInfoPOne, newInfoPTwo;
    
    void Update()
    {
        playerOneCurrent.newData = newInfoPOne;
        playerTwoCurrent.newData = newInfoPTwo;

        if (playerOneCurrent.newData)
        {
            playerOneCurrent.newData = false;
        }
    }

    void CheckInput()
    {
        
    }
}
