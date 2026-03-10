using UnityEngine;
using System.Collections;
using DG.Tweening;
using JetBrains.Annotations;

public class MagicBall : MonoBehaviour
{
    public GameObject topHalfSphere;
    public GameObject bottomHalfSphere;
    private float initialXRotation;
    private float initialZRotation;

    [Header("Animation Settings")]
    public float rotationSpeed = 100f;

    public float moveUpDistance = 0.07f;
    public float bobbingAmplitude = 0.5f;
    public float bobbingFrequency = 1f;

    public float animationSpeed = 0.2f;

    [Header("Emission Settings")]
    [ColorUsage(true, true)] public Color idleEmission = Color.black;
    [ColorUsage(true, true)] public Color castEmission = Color.white * 10f;
    private Material emissionTopMaterial; // Assign this in the inspector with the material that has emission
    private Material emissionBotMaterial; // This will be assigned at runtime
    public float emissionStrength = 8f; // Adjust this value to control the strength of the emission

    [Header("Casting Settings")]
    public float castDuration = 0.4f;
    public float anticipationTime = 0.06f;

    [Header("Camera Shake")]
    public float cameraShakeStrength = 0.08f;
    public float cameraDuration = 0.15f;
    public int cameraVibrato = 20;

    [Header("Punch Effect")]
    public float punchAmount = 0.2f;
    public float duration = 0.15f;
    public int vibrato = 10;
    public float elasticity = 1f;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        emissionTopMaterial = topHalfSphere.GetComponent<Renderer>().material; // Get the material from the top half sphere
        emissionBotMaterial = bottomHalfSphere.GetComponent<Renderer>().material; // Get the material from the bottom half sphere

        initialXRotation = topHalfSphere.transform.localEulerAngles.x;
        initialZRotation = topHalfSphere.transform.localEulerAngles.z;
        
        emissionTopMaterial.SetColor("_EmissionColor", Color.black); // Set initial emission strength
        emissionBotMaterial.SetColor("_EmissionColor", Color.black); // Set initial emission strength
        StartCoroutine(InitializeSequence());

        
    }

    // Update is called once per frame
    void Update()
    {
        // local bobbing effect for the magic ball
        float bobbingOffset = Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
        transform.localPosition = new Vector3(transform.localPosition.x, bobbingOffset, transform.localPosition.z);




    }

    private IEnumerator InitializeSequence()
    {
        while (true)
        {
        // Wait for 1 second before opening the magic ball
        yield return new WaitForSeconds(1f);
        Open();
        yield return new WaitForSeconds(2f);
        RecieveSpell(90f); // Example rotation amount, you can adjust this as needed
        yield return new WaitForSeconds(1f);
        RecieveSpell(90f); // Example rotation amount, you can adjust this as needed
        yield return new WaitForSeconds(1f);
        RecieveSpell(-180f); // Example rotation amount, you can adjust this as needed
        yield return new WaitForSeconds(1f);
        RecieveSpell(180f); // Example rotation amount, you can adjust this as needed
        // Wait for 2 seconds before closing the magic ball
        yield return new WaitForSeconds(2f);
        Close();

        yield return new WaitForSeconds(1f);
        SpellCasted();
        }
    }

    public void Open()
    {
        topHalfSphere.transform.DOLocalMove(new Vector3(0f, moveUpDistance, 0f), animationSpeed).SetEase(Ease.OutBack);   
        emissionTopMaterial.SetColor("_EmissionColor", Color.black); // Fade out the emission
        emissionBotMaterial.SetColor("_EmissionColor", Color.black); // Fade out the emission
    }

    public void Close()
    {
        topHalfSphere.transform.DOLocalMove(Vector3.zero, animationSpeed).SetEase(Ease.OutBack);   

        emissionBotMaterial.SetColor("_EmissionColor", Color.white * emissionStrength); // Fade in the emission 
        emissionTopMaterial.SetColor("_EmissionColor", Color.white * emissionStrength); // Fade in the emission
    }

    public void RecieveSpell(float rotationAmount)
    {
        var seq = DOTween.Sequence();

        // 1) Anticipation
        seq.AppendInterval(anticipationTime);
        seq.Append(topHalfSphere.transform.DOPunchScale(Vector3.one * punchAmount, duration, vibrato, elasticity));

        // 2) Impact + spin
        seq.Append(topHalfSphere.transform
            .DOLocalRotate(new Vector3(0f, rotationAmount, 0f), castDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutBack)
            .OnUpdate(() =>
            {
                var e = topHalfSphere.transform.localEulerAngles;
                topHalfSphere.transform.localEulerAngles = new Vector3(initialXRotation, e.y, initialZRotation);
            }));

        // 3) Emission flash
        seq.Join(DOTween.To(
            () => emissionBotMaterial.GetColor("_EmissionColor"),
            c => emissionBotMaterial.SetColor("_EmissionColor", c),
            castEmission,
            animationSpeed
        ));
        seq.Join(DOTween.To(
            () => emissionTopMaterial.GetColor("_EmissionColor"),
            c => emissionTopMaterial.SetColor("_EmissionColor", c),
            castEmission,
            animationSpeed
         ));
        seq.OnComplete(() =>
        {
            DOTween.To(
                () => emissionTopMaterial.GetColor("_EmissionColor"),
                c => emissionTopMaterial.SetColor("_EmissionColor", c),
                idleEmission,
                animationSpeed
            );
            DOTween.To(
                () => emissionBotMaterial.GetColor("_EmissionColor"),
                c => emissionBotMaterial.SetColor("_EmissionColor", c),
                idleEmission,
                animationSpeed
            );
        });

        // 4) World reaction
        if (Camera.main != null)
            Camera.main.transform.DOShakePosition(cameraDuration, cameraShakeStrength, cameraVibrato, 90f, false, true);
        
    }

    public void SpellCasted()
    {
        // This method can be called when the spell is successfully casted, you can add additional effects or logic here if needed
        // reset the magic ball to its idle state
        DOTween.To(
                () => emissionTopMaterial.GetColor("_EmissionColor"),
                c => emissionTopMaterial.SetColor("_EmissionColor", c),
                idleEmission,
                animationSpeed
            );
            DOTween.To(
                () => emissionBotMaterial.GetColor("_EmissionColor"),
                c => emissionBotMaterial.SetColor("_EmissionColor", c),
                idleEmission,
                animationSpeed
            );
        
            StartCoroutine(InitializeSequence()); // Restart the sequence for demonstration purposes, you can remove this if you don't want it to loop
    }

}
