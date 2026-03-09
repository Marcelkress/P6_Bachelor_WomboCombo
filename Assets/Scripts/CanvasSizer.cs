using UnityEngine;
using UnityEngine.UI;

public class CanvasSizer : MonoBehaviour
{
    public float valueToAdd = 450;
    private Enemy enemyScript;
    private Canvas canvas;
        
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvas = GetComponent<Canvas>();
        enemyScript = GetComponentInParent<Enemy>();
        int comboSize = enemyScript.comboLength;

        float size = canvas.GetComponent<RectTransform>().rect.width;
        
        for (int i = 1; i < comboSize; i++)
        {
            size += 450;
        }

        canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
    }

    
}
