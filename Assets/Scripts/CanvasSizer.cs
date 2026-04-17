using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
public class CanvasSizer : MonoBehaviour
{
    [SerializeField] public float valueToAdd = 450;
    private Enemy enemyScript;
    [SerializeField] public bool useBossLogic = false, useProjectileLogic = false;
    [SerializeField] UniCanvasSizerSettings canvasSettings;
    private BossComboSystem bossComboSystem;
    private EnemyProjectile enemyProjectile;
    private Canvas canvas;
    private CanvasGroup content;
    public Image borderImage;

    private int comboSize;

    public Color uiBorderColor;
    private Vector3 currentCanvasSize;
    private Vector3 targetCanvasSize;
    private Vector3 farCanvasSize;

    private float initialCanvasDistance;

    private GameObject player;  

    private void Awake()
    {
        content = GetComponent<CanvasGroup>();
        content.alpha = 0;
    }
        
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        canvas = GetComponent<Canvas>();
        currentCanvasSize = canvas.transform.localScale;

        StartCoroutine(FadeCanvasIn());

        initialCanvasDistance = Vector3.Distance(player.transform.position, transform.position);
        targetCanvasSize = currentCanvasSize/canvasSettings.reducingFactor;
        farCanvasSize = currentCanvasSize * canvasSettings.maxFarScaleMultiplier;
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

    private IEnumerator FadeCanvasIn()
    {
        if (useBossLogic)
        {
            yield break;
        }
        yield return new WaitForSeconds(canvasSettings.canvasFadeInTime / 2);
        if (useProjectileLogic)
        {
            borderImage.color = enemyProjectile.uiBorderColor;
        }
        else
        {
            borderImage.color = enemyScript.uiBorderColor;
        }
        content.DOFade(1, canvasSettings.canvasFadeInTime);
    }

    // Sizes the canvas based on the distance to the player, making it smaller as the player gets closer
    private void Update()
    {
        Vector3 playerPos = player.transform.position;

        float dis = Vector3.Distance(playerPos, transform.position);

        Vector3 newCurrentCanvasSize;

        if (dis <= canvasSettings.startScalingDistance)
        {
            float clampedDistance = Mathf.Clamp01(dis / canvasSettings.startScalingDistance);
            newCurrentCanvasSize = Vector3.Lerp(targetCanvasSize, currentCanvasSize, clampedDistance);
        }
        else
        {
            float clampedDistance = Mathf.InverseLerp(canvasSettings.startScalingDistance, canvasSettings.farScaleDistance, dis);
            newCurrentCanvasSize = Vector3.Lerp(currentCanvasSize, farCanvasSize, clampedDistance);
        }

        canvas.transform.localScale = newCurrentCanvasSize;
    }
    
}
