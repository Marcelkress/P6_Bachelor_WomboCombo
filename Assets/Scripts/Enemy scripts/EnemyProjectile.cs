using System;
using DG.Tweening;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Transform target;
    public int damage = 1;
    public float speedTime;

    public float liveTime = 10f;
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
        
    }
}
