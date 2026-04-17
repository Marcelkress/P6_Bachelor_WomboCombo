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
    public bool useBalls;
    public bool autoSpawnPlayer2;
    public GameObject player2Prefab;

    
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
        
        if(useBalls)
            controllerInput = FindFirstObjectByType<ControllerInputCrossPlatform>();
    }

    void Start()
    {
        if (autoSpawnPlayer2)
        {
            GameObject player2 = Instantiate(player2Prefab, transform.position, transform.rotation);
        }
    }

    
    private void Update()
    {
        if (useBalls)
        inputData = controllerInput.GetInput(playerID == Player.Player1 ? 1 : 2);

        if (!inputData.connected)
            return;

        controllerValue = inputData.rotation;
        UpdateSymbolsFromController();

        if (inputData.pushPending)
        {
            OnFire();
        }
    }


    
    private bool IsInInterval(Interval interval)
    {
        // Handles wrap-around case (e.g. min=29, max=4)
        if (interval.min > interval.max)
            return controllerValue >= interval.min || controllerValue <= interval.max;
        else
            return controllerValue >= interval.min && controllerValue <= interval.max;
    }
    
    private bool IsMoonMoon() => IsInInterval(moonMoon);
    private bool IsMoonStar() => IsInInterval(moonStar);
    private bool IsMoonSun()  => IsInInterval(moonSun);
    private bool IsStarMoon() => IsInInterval(starMoon);
    private bool IsStarStar() => IsInInterval(starStar);
    private bool IsStarSun()  => IsInInterval(starSun);
    private bool IsSunMoon()  => IsInInterval(sunMoon);
    private bool IsSunStar()  => IsInInterval(sunStar);
    private bool IsSunSun()   => IsInInterval(sunSun);

    private void UpdateSymbolsFromController()
    {
        if (playerID == Player.Player1)
        {
            if (IsSunMoon())  { playerInfoStruct.symbOne = sun;  playerInfoStruct.symbTwo = moon; }
            else if (IsSunStar())  { playerInfoStruct.symbOne = sun;  playerInfoStruct.symbTwo = star; }
            else if (IsSunSun())   { playerInfoStruct.symbOne = sun;  playerInfoStruct.symbTwo = sun;  }
        }
        else if (playerID == Player.Player2)
        {
            if (IsMoonMoon())      { playerInfoStruct.symbOne = moon; playerInfoStruct.symbTwo = moon; }
            else if (IsMoonStar()) { playerInfoStruct.symbOne = moon; playerInfoStruct.symbTwo = star; }
            else if (IsMoonSun())  { playerInfoStruct.symbOne = moon; playerInfoStruct.symbTwo = sun;  }
        }

        if (IsStarMoon()) { playerInfoStruct.symbOne = star; playerInfoStruct.symbTwo = moon; }
        else if (IsStarStar()) { playerInfoStruct.symbOne = star; playerInfoStruct.symbTwo = star; }
        else if (IsStarSun())  { playerInfoStruct.symbOne = star; playerInfoStruct.symbTwo = sun;  }

        //Debug.Log(playerInfoStruct.symbOne + "    " + playerInfoStruct.symbTwo);
    }

    public PlayerInfoStruct GetSymbolUpdate()
    {
        return playerInfoStruct;
    }
    
    public void OnFire()
    {
        InputManager.instance.UpdatePlayerInfo((int)playerID, playerInfoStruct);
        //Debug.Log("Fire from combo input " + playerID.ToString());
    }

    private bool topCanChange = true, bottomCanChange = true;

    public void OnTopCycle(InputValue value)
    {
        var val = value.Get<Vector2>();

        if ((val.x < -.5 || val.x > .5) && topCanChange)
        {
            topCanChange = false;
            playerInfoStruct.symbOne += (int)val.x;

            if (playerID == Player.Player2)
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
            else if (playerID == Player.Player1)
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
    public bool pushPending; // set by serial thread on rising edge, cleared by main thread after reading
}
