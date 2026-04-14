using UnityEngine;
using UnityEngine.UI;

public class CanvasSizer : MonoBehaviour
{
    public float valueToAdd = 450;
    private Enemy enemyScript;
    public bool useBossLogic = false, useProjectileLogic = false;
    public int scalingFactor = 2;
    [SerializeField] private float startScalingDistance = 30f;
    [SerializeField] private float maxFarScaleMultiplier = 1.5f;
    [SerializeField] private float farScaleDistance = 90f;
    private BossComboSystem bossComboSystem;
    private EnemyProjectile enemyProjectile;
    private Canvas canvas;

    private int comboSize;

    private Vector3 currentCanvasSize;
    private Vector3 targetCanvasSize;
    private Vector3 farCanvasSize;

    private float initialCanvasDistance;

    private GameObject player;
        
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        canvas = GetComponent<Canvas>();
        currentCanvasSize = canvas.transform.localScale;

        initialCanvasDistance = Vector3.Distance(player.transform.position, transform.position);
        targetCanvasSize = currentCanvasSize/scalingFactor;
        farCanvasSize = currentCanvasSize * maxFarScaleMultiplier;
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
            size += valueToAdd;
        }

        canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size); 
    }

    // Sizes the canvas based on the distance to the player, making it smaller as the player gets closer
    private void Update()
    {
        Vector3 playerPos = player.transform.position;

        float dis = Vector3.Distance(playerPos, transform.position);

        Vector3 newCurrentCanvasSize;

        if (dis <= startScalingDistance)
        {
            float clampedDistance = Mathf.Clamp01(dis / startScalingDistance);
            newCurrentCanvasSize = Vector3.Lerp(targetCanvasSize, currentCanvasSize, clampedDistance);
        }
        else
        {
            float clampedDistance = Mathf.InverseLerp(startScalingDistance, farScaleDistance, dis);
            newCurrentCanvasSize = Vector3.Lerp(currentCanvasSize, farCanvasSize, clampedDistance);
        }

        canvas.transform.localScale = newCurrentCanvasSize;
    }
    
}
