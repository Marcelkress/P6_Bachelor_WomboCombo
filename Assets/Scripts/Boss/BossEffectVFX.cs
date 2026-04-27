using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using Unity.VisualScripting;
using DG.Tweening;

public class BossEffectVFX : MonoBehaviour
{
    public ParticleSystem smokeEffect;
    public Transform smokeTargetPos;
    public GameObject[] lightningEffects;

    public float smokeDelay = 1f;
    private RFX4_EffectSettings fireEffectSettings;

    public float smokeRiseHeight = 4f;
    public float smokeRiseDuration = 2f;

    public float fireEffectDuration = 1f;
    
    void Awake()
    {
        fireEffectSettings = GetComponent<RFX4_EffectSettings>();

        foreach (var lightning in lightningEffects)
        {
            lightning.gameObject.SetActive(false);
        }
    }

    private bool stopped = false;
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(smokeDelay);
        smokeEffect.Play();

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(PlayRandomLightningEffects());

        smokeEffect.gameObject.transform.DOMoveY(smokeEffect.transform.position.y + smokeRiseHeight, smokeRiseDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            
            stopped = true;

            smokeEffect.gameObject.transform.DOMove(smokeTargetPos.position, 1f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                smokeEffect.Stop();
            });
           
            
        });
        yield return new WaitForSeconds(fireEffectDuration+smokeRiseDuration);
        fireEffectSettings.IsVisible = false;
    }

    private IEnumerator PlayRandomLightningEffects()
    {
        while (stopped == false)
        {
            int randomIndex = Random.Range(0, lightningEffects.Length);
            lightningEffects[randomIndex].gameObject.SetActive(true);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            lightningEffects[randomIndex].gameObject.SetActive(false);
        }
    }
}
