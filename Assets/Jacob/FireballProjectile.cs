using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;

public class FireballProjectile : MonoBehaviour
{
    public float speed = 10f;

    public AnimationCurve projectileCurve;
    private Transform target;

    private Transform currentPosition;

    private float animationTime;

    public GameObject fireExplosionPrefab;

    private Enemy targetedEnemyScript;
    private void Start()
    {
        currentPosition = this.transform;
    }
    public void SetTargetTransform(Transform targetTransform, GameObject targetedEnemy)
    {
        targetedEnemyScript = targetedEnemy.GetComponent<Enemy>();
        
        if (target == null)
        {
            target = targetTransform;
        }
        
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        animationTime += Time.deltaTime * speed;        
        currentPosition.position = Vector3.MoveTowards(currentPosition.position, target.position, projectileCurve.Evaluate(animationTime));
        
        float distance = Vector3.Distance(currentPosition.position, target.position);

        if (distance <= 0.01f) // Distance to register as hit.
        {
            targetedEnemyScript.UpdateUI();
            // Epic explosion vfx
            GameObject explosion = Instantiate(fireExplosionPrefab, target.position, target.rotation);
            this.gameObject.SetActive(false);
        }
    }
}
