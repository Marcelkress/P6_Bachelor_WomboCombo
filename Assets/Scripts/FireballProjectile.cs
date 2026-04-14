using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    public float speed = 10f;
    public AnimationCurve projectileCurve;
    private Transform target;
    public float maxLiveTime = 20;

    private Transform currentPosition;
    private float animationTime;
    public GameObject fireExplosionPrefab;

    private IEnemyDamagable targetedEnemyScript;

    [SerializeField] private AK.Wwise.Event playerProjectileSound;
    private uint _soundPlayingId;

    private void OnEnable()
    {
        currentPosition = this.transform;
        animationTime = 0f;
        timer = 0f;
        _soundPlayingId = playerProjectileSound.Post(gameObject);
    }

    private void OnDisable()
    {
        StopProjectileSound();
    }

    private void OnDestroy()
    {
        StopProjectileSound();
    }

    public void SetTargetTransform(Transform targetTransform, GameObject targetedEnemy)
    {
        targetedEnemyScript = targetedEnemy.GetComponent<IEnemyDamagable>();
        if (target == null)
        {
            target = targetTransform;
        }
    }

    private float timer;

    void FixedUpdate()
    {
        if (target == null || currentPosition == null)
        {
            this.gameObject.SetActive(false);
            return;
        }

        animationTime += Time.deltaTime * speed;
        currentPosition.position = Vector3.MoveTowards(
            currentPosition.position,
            target.position,
            projectileCurve.Evaluate(animationTime)
        );

        float distance = Vector3.Distance(currentPosition.position, target.position);

        if (distance <= 0.01f)
        {
            targetedEnemyScript.UpdateUI();
            Instantiate(fireExplosionPrefab, target.position, target.rotation);
            target = null;
            this.gameObject.SetActive(false);
            return;
        }

        timer += Time.deltaTime;
        if (timer > maxLiveTime)
        {
            this.gameObject.SetActive(false);
        }
    }

    private void StopProjectileSound()
    {
        if (_soundPlayingId != 0)
        {
            AkSoundEngine.StopPlayingID(_soundPlayingId, 0);
            _soundPlayingId = 0;
        }
    }
}

public interface IEnemyDamagable
{
    public void UpdateUI();
}