using System;
using DG.Tweening;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Transform target;
    public int damage = 1;
    public float speedTime;
    public float liveTime = 10f;
    private float timer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = Camera.main.transform;
        transform.DOMove(target.position, speedTime, false).SetEase(Ease.InOutCubic);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.transform.CompareTag("Player"))
        {
            other.transform.GetComponent<Player>().TakeDamage(damage);
            Destroy(this);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > liveTime)
        {
            Destroy(this);
        }
    }
}
