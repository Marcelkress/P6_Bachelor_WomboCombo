using UnityEngine;
using System.Collections;
using DG.Tweening;

public class MagicBall : MonoBehaviour
{
    public GameObject topHalfSphere;
    public GameObject bottomHalfSphere;
    private float initialXRotation;
    private float initialZRotation;

    [Header("Animation Settings")]
    public float rotationSpeed = 100f;
    public float idleRotationVariation = 0.25f;
    public float bottomHalfRotationMultiplier = -1f;
    public float spellAttackSpeed = 15f;
    public float spellHitDistance = 0.15f;
    public float spellTargetYOffset = 0.75f;
    public bool attachExplosionToTarget = true;

    public float moveUpDistance = 0.07f;
    public float bobbingAmplitude = 0.5f;
    public float bobbingFrequency = 1f;
    public Transform bobbingTransform; // Assign the transform you want to bob in the inspector
    public Transform returnPoint; // Assign the point to return to after attack in the inspector
    public float animationSpeed = 0.2f;

    [Header("vfx Effects")]
    public ParticleSystem fireParticles;
    public ParticleSystem fireEmbersParticles;
    public Material fireDecalMaterial;

    [Header("Epic Explosion")]
    public GameObject epicExplosionPrefab;
    public float epicExplosionShakeStrength = 1f;
    public float epicExplosionShakeDuration = 2f;


    
    [Header("Emission Settings")]
    [ColorUsage(true, true)] public Color baseEnergyColor = Color.white;
    [ColorUsage(true, true)] public Color maxEnergyColor = new Color(1f, 0.22f, 0.05f);
    private Material emissionTopMaterial; // Assign this in the inspector with the material that has emission
    private Material emissionBotMaterial; // This will be assigned at runtime
    public float emissionStrength = 8f; // Adjust this value to control the strength of the emission
    public float idleEmissionMultiplier = 0.25f;
    public float closeEmissionMultiplier = 1f;
    public float flashEmissionMultiplier = 1.5f;
    public float colorBlendDuration = 0.2f;

    [Header("Casting Settings")]
    public float castDuration = 0.4f;
    public float anticipationTime = 0.06f;
    public float openAfterCastDelay = 0.05f;

    [Header("Camera Shake")]
    public Transform shakeTransform, bigBoomShaker; // Assign the camera transform in the inspector
    public float cameraShakeStrength = 0.08f;
    public float cameraDuration = 0.15f;
    public int cameraVibrato = 20;

    [Header("Punch Effect")]
    public float punchAmount = 0.2f;
    public float duration = 0.15f;
    public int vibrato = 10;
    public float elasticity = 1f;

    private Vector3 topHalfInitialScale;
    private Vector3 initialShakeLocalPosition;
    private Vector3 initialBigBoomLocalPosition;
    private Vector3 initialLocalPosition;
    private Coroutine castRoutine;
    private bool isCasting;
    private float currentEpicness01;
    private float currentEmissionMultiplier;
    private Tween emissionTween;
    private Tween colorTween;

    public Player playerScript; // Reference to the Player script, assign in inspector
    private Color currentColor;

    private Vector3 initialPos;

    [Header("Bomb Timer sound")] [SerializeField]
    private AK.Wwise.Event BombTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        fireDecalMaterial.SetFloat("_CutoutAlphaMul", 0f); // Ensure the fire decal starts invisible

        initialPos = transform.position;
        emissionTopMaterial = topHalfSphere.GetComponent<Renderer>().material; // Get the material from the top half sphere
        emissionBotMaterial = bottomHalfSphere.GetComponent<Renderer>().material; // Get the material from the bottom half sphere

        initialXRotation = topHalfSphere.transform.localEulerAngles.x;
        initialZRotation = topHalfSphere.transform.localEulerAngles.z;

        currentColor = baseEnergyColor;
        currentEmissionMultiplier = idleEmissionMultiplier;
        ApplyEmissionColor();
        //StartCoroutine(InitializeSequence());
        
       

        topHalfInitialScale = topHalfSphere.transform.localScale; // Store the initial scale of the top half sphere
        initialLocalPosition = transform.localPosition;

        if (shakeTransform != null)
            initialShakeLocalPosition = shakeTransform.localPosition; // Store the baseline local position for regular shake

        if (bigBoomShaker != null)
            initialBigBoomLocalPosition = bigBoomShaker.localPosition; // Store the baseline local position for big boom shake

        if (playerScript != null)
        {
            playerScript.EpicnessChanged += OnEpicnessChanged;
            OnEpicnessChanged(playerScript.currentEpicness);
        }

        Open(); // Stage 1 should be open by default
    }

    private void OnDisable()
    {
        if (castRoutine != null)
        {
            StopCoroutine(castRoutine);
            castRoutine = null;
        }

        if (emissionTween != null && emissionTween.IsActive())
            emissionTween.Kill();

        if (colorTween != null && colorTween.IsActive())
            colorTween.Kill();

        if (playerScript != null)
            playerScript.EpicnessChanged -= OnEpicnessChanged;

        isCasting = false;
    }

    // Update is called once per frame
    void Update()
    {
        // local bobbing effect for the magic ball
        float bobbingOffset = Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
        bobbingTransform.localPosition = new Vector3(initialLocalPosition.x, initialLocalPosition.y + bobbingOffset, initialLocalPosition.z);

        if (!isCasting)
        {
            float variation = 1f + (Mathf.PerlinNoise(Time.time * 0.8f, 0.17f) - 0.5f) * 2f * idleRotationVariation;
            float topRotationStep = rotationSpeed * variation * Time.deltaTime;
            float bottomRotationStep = rotationSpeed * variation * bottomHalfRotationMultiplier * Time.deltaTime;

            topHalfSphere.transform.Rotate(Vector3.up, topRotationStep, Space.Self);
            bottomHalfSphere.transform.Rotate(Vector3.up, bottomRotationStep, Space.Self);
        }

    }
    private void ApplyEmissionColor()
    {
        Color emissionColor = currentColor * (currentEmissionMultiplier * emissionStrength);
        emissionTopMaterial.SetColor("_EmissionColor", emissionColor);
        emissionBotMaterial.SetColor("_EmissionColor", emissionColor);


    }

    private Tween TweenEmissionTo(float targetMultiplier, float duration, Ease ease = Ease.OutSine)
    {
        if (emissionTween != null && emissionTween.IsActive())
            emissionTween.Kill();

        emissionTween = DOTween.To(
                () => currentEmissionMultiplier,
                value =>
                {
                    currentEmissionMultiplier = value;
                    ApplyEmissionColor();
                },
                targetMultiplier,
                duration)
            .SetEase(ease);

        return emissionTween;
    }

    public void ErrorFeedback()
    {
        //TODO: gør farven på emission rød og shake bolden fra side til side
    }

    public void Open()
    {
        topHalfSphere.transform.DOLocalMove(new Vector3(0f, moveUpDistance, 0f), animationSpeed).SetEase(Ease.OutBack);   
        TweenEmissionTo(idleEmissionMultiplier, animationSpeed);
    }

    public void Close()
    {
        topHalfSphere.transform.DOLocalMove(Vector3.zero, animationSpeed).SetEase(Ease.OutBack);   
        TweenEmissionTo(closeEmissionMultiplier, animationSpeed);
    }

    public void RecieveSpell()
    {
        var seq = DOTween.Sequence();

        // 3) Emission flash
        if (emissionTween != null && emissionTween.IsActive())
            emissionTween.Kill();

        seq.Append(DOTween.To(
            () => currentEmissionMultiplier,
            value =>
            {
                currentEmissionMultiplier = value;
                ApplyEmissionColor();
            },
            flashEmissionMultiplier,
            animationSpeed
        ));

        seq.Append(DOTween.To(
            () => currentEmissionMultiplier,
            value =>
            {
                currentEmissionMultiplier = value;
                ApplyEmissionColor();
            },
            idleEmissionMultiplier,
            animationSpeed
        ));

        emissionTween = seq;
        topHalfSphere.transform.localScale = topHalfInitialScale; // Reset the scale to prevent cumulative scaling from punch effect


        // 4) World reaction
        if (shakeTransform != null)
        {
            // shake local position of the camera to create a punch effect
            shakeTransform.DOKill();

            shakeTransform.DOShakePosition(cameraDuration, cameraShakeStrength, cameraVibrato, 90f, false, true).OnComplete(() =>
            {
                if (shakeTransform != null)
                    shakeTransform.localPosition = initialShakeLocalPosition; // Reset camera to its own baseline
            })
            ;
        }
           
    }

    private void OnEpicnessChanged(float currentEpicness)
    {
        // Store normalized value for reactive tuning without polling in Update.
        currentEpicness01 = Mathf.Clamp01(currentEpicness / Mathf.Max(0.0001f, playerScript.maxEpicness));
        Color targetColor = Color.Lerp(baseEnergyColor, maxEnergyColor, currentEpicness01);

        if (colorTween != null && colorTween.IsActive())
            colorTween.Kill();

        colorTween = DOTween.To(
                () => currentColor,
                value =>
                {
                    currentColor = value;
                    ApplyEmissionColor();
                },
                targetColor,
                colorBlendDuration)
            .SetEase(Ease.OutSine);
        
        if (currentEpicness01 >= playerScript.epicnessThresholdForSpell - 0.25f)
        {
            fireParticles.Play();
            fireEmbersParticles.Play();
            if (fireDecalMaterial != null)
            {
                // fade in _CutoutAlphaMul
                fireDecalMaterial.DOFloat(2f, "_CutoutAlphaMul", colorBlendDuration).SetEase(Ease.OutSine);
            }
        }
        else
        {
            fireParticles.Stop();
            fireEmbersParticles.Stop();
            if (fireDecalMaterial != null)
            {
                // fade out _CutoutAlphaMul
                fireDecalMaterial.DOFloat(0f, "_CutoutAlphaMul", colorBlendDuration).SetEase(Ease.OutSine);
            }
        }
    }

    public void PlayCastFeedback()
    {
        if (castRoutine != null)
            StopCoroutine(castRoutine);

        castRoutine = StartCoroutine(PlayCastFeedbackSequence());
    }

    private IEnumerator PlayCastFeedbackSequence()
    {
        isCasting = true;

        Close();
        yield return new WaitForSeconds(anticipationTime);

        RecieveSpell();
        yield return new WaitForSeconds(castDuration);

        SpellCasted();
        yield return new WaitForSeconds(openAfterCastDelay);

        Open();
        isCasting = false;
        castRoutine = null;
    }

    public void SpellCasted()
    {
        // This method can be called when the spell is successfully casted, you can add additional effects or logic here if needed
        // reset the magic ball to its idle state
        TweenEmissionTo(idleEmissionMultiplier, animationSpeed);
        
           // StartCoroutine(InitializeSequence()); // Restart the sequence for demonstration purposes, you can remove this if you don't want it to loop
    }
    private bool attacking = false;
    public IEnumerator SpellAttack(GameObject target, bool resetReachedTarget)
    {
        attacking = resetReachedTarget; // Set the flag based on the parameter to allow resetting it for new attacks

        if (!attacking)
            yield break;

        if (target == null)
        {
            yield return StartCoroutine(returnMagicBall(explosion: null));
            yield break;
        }

        Collider targetCollider = target.GetComponentInChildren<Collider>();
        IEnemyDamagable targetDamageable = target.GetComponent<IEnemyDamagable>();

        Debug.Log(targetDamageable != null ? "Target is damageable" : "Target is NOT damageable");

        while (attacking)
        {
            if (target == null)
            {
                attacking = false;
                break;
            }

            Vector3 targetPos = targetCollider != null
                ? targetCollider.bounds.center
                : target.transform.position + Vector3.up * spellTargetYOffset;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, spellAttackSpeed * Time.deltaTime);
            
            
            // when close enought use the epicExplosionParticle and then the magic ball returns to its initial position
            if (Vector3.Distance(transform.position, targetPos) < spellHitDistance)
            {
                if (attacking) // Only trigger the explosion and reset if we haven't already reached the target
                {
                    GameObject explosion = null;

                    if (epicExplosionPrefab != null)
                    {
                        explosion = Instantiate(epicExplosionPrefab, targetPos, Quaternion.identity);
                        BombTimer.Stop(this.gameObject);

                        if (attachExplosionToTarget && target != null) // so it is always on the target
                            explosion.transform.SetParent(target.transform, true);

                    }

                    targetDamageable?.UpdateUI(true); // Pass true to indicate an instant kill for UI update
                    if (bigBoomShaker != null)
                    {
                        bigBoomShaker.DOKill();
                        bigBoomShaker.DOShakePosition(epicExplosionShakeDuration, epicExplosionShakeStrength, cameraVibrato, 90f, false, true).OnComplete(() =>
                        {
                            if (bigBoomShaker != null)
                                bigBoomShaker.localPosition = initialBigBoomLocalPosition; // Reset big boom shaker to its own baseline
                        });
                    }

                    yield return new WaitForSeconds(0.2f); // Wait for the explosion effect to play
                    attacking = false; // Set the flag to false to stop the attack loop
                    yield return StartCoroutine(returnMagicBall(explosion)); // Start returning the magic ball to its initial position
                    yield break;
                }
            }
            yield return null;
        }

        yield return StartCoroutine(returnMagicBall(explosion: null)); // Ensure the magic ball returns to its initial position if the attack was interrupted

    }

    public IEnumerator returnMagicBall(GameObject explosion)
    {
        if (returnPoint == null)
        {
            transform.position = initialPos;
            yield break;
        }

        while (Vector3.Distance(transform.position, returnPoint.position) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, returnPoint.position, spellAttackSpeed * Time.deltaTime);
            if (explosion != null)     
            {
            Destroy(explosion, 3f); // Ensure the explosion is destroyed if it's still around while returning
            }
            yield return null;
        }
        transform.position = returnPoint.position; // Ensure it snaps exactly to the return point
    }

    public void BombTimerStart()
    {
        BombTimer.Post(this.gameObject);
    }
    
    
    
}
