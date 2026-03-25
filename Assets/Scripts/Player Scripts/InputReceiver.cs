using UnityEngine;

public struct InputStruct
{
    public int ID;
    public int rotationVal;
    public int pressVa;
}

public class InputReceiver : MonoBehaviour
{
    public InputStruct playerOne, playerTwo;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // playerOne.rotationVal = serialinput.rotation; 
        
        // if ( no  new serial data)
        //      pressVal = 0; 
        
    }
}
