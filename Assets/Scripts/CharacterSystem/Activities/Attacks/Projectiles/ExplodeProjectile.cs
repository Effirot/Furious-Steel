

using CharacterSystem.DamageMath;
using Unity.Cinemachine;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System;

public class ExplodeProjectile : Projectile
{
    [Space]
    [Header("Explode")]
    [SerializeField, Range(0, 10)]
    private float Range = 2;

    [SerializeField, Range(0, 10)]
    private float PushForce = 0.6f;

    [SerializeField]
    private Damage explodeDamage;

    [field : SerializeField]
    public GameObject OnExplodePrefab { get; private set; }

    [SerializeField, Range(0, 5)]
    private float PereodicExplodeDelay = 0;

    private float explodeTimer = 0;

    public override void Kill(Damage damage)
    {
        if (isServer)
        {
            ExplodeSynced();

            Explode();
        }

        base.Kill(damage);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (PereodicExplodeDelay > 0)
        {
            explodeTimer += Time.fixedDeltaTime;

            if (explodeTimer > PereodicExplodeDelay)
            {
                base.Kill(default);

                explodeTimer = 0;
            }
        }
    }

    [Server, ClientRpc]
    public void ExplodeSynced()
    {
        if (!isServer)
        {
            Explode();
        }
    }

    public void Explode()
    {
        if (!OnExplodePrefab.IsUnityNull())
        {
            var gameObject = Instantiate(OnExplodePrefab, transform.position, transform.rotation);

            gameObject.SetActive(true);

            gameObject.GetComponent<CinemachineImpulseSource>()?.GenerateImpulse();

            Destroy(gameObject, 4);
        }

        if (Summoner.IsUnityNull()) 
            return;

        if (isServer)
        {
            foreach (var collider in Physics.OverlapSphere(transform.position, Range))
            {
                var damage = explodeDamage;

                damage.sender = Summoner;
                damage.pushDirection = collider.transform.position - transform.position;
                damage.pushDirection.Normalize();
                damage.pushDirection *= PushForce;

                var originalPushDirection = transform.rotation * explodeDamage.pushDirection;
                damage.pushDirection.y = Mathf.Max(damage.pushDirection.y, originalPushDirection.y);

                var report = Damage.Deliver(collider.gameObject, damage);
                
                onDamageDeliveryReport?.Invoke(report);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}