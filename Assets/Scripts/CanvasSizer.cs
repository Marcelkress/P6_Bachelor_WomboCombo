using UnityEngine;
using UnityEngine.UI;

public class CanvasSizer : MonoBehaviour
{
    public float valueToAdd = 450;
    private Enemy enemyScript;
    public bool useBossLogic = false, useProjectileLogic = false;
    private BossComboSystem bossComboSystem;
    private EnemyProjectile enemyProjectile;
    private Canvas canvas;

    private int comboSize;
        
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (useBossLogic)
        {
            bossComboSystem = GetComponentInParent<BossComboSystem>();
            comboSize = bossComboSystem.bossComboArray.Length / 2;
        }
        else if (useProjectileLogic)
        {
            enemyProjectile = GetComponentInParent<EnemyProjectile>();
            comboSize = enemyProjectile.comboArray.Length / 2;
        }
        else
        {
            enemyScript = GetComponentInParent<Enemy>();
            comboSize = enemyScript.comboLength;
        }

        canvas = GetComponent<Canvas>();
        float size = canvas.GetComponent<RectTransform>().rect.width;
        
        for (int i = 1; i < comboSize; i++)
        {
            size += 450;
        }

        canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size); 
    }

    
}
