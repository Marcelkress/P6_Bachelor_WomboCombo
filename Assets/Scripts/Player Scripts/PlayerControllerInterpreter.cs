using UnityEngine;

public class PlayerControllerInterpreter : MonoBehaviour
{
    private PlayerComboInput comboInput;

    public int controllerValue;
    
    private int minVal = 0, maxVal = 127;

    [Header("Sun interval")] public int sunMin;
    public int sunMax;

    [Header("Moon interval")] public int moonMin;
    public int moonMax;
    
    [Header("Star interval")] public int starMin;
    public int starMax;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float val = Mathf.Clamp(controllerValue, minVal, maxVal);

        if (val < sunMax && val > sunMin)
        {
            Debug.Log("SUN!");
        }
        else if()
        {
            
        }
    }
}
