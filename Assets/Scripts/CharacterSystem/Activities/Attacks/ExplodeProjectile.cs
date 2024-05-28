

using CharacterSystem.DamageMath;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ExplodeProjectile : Projectile
{
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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        Explode_ClientRpc();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (PereodicExplodeDelay > 0)
        {
            explodeTimer += Time.fixedDeltaTime;

            if (explodeTimer > PereodicExplodeDelay)
            {
                Explode_ClientRpc();

                explodeTimer = 0;
            }
        }
    }

    [ClientRpc]
    private void Explode_ClientRpc()
    {
        Explode();
    }
    private void Explode()
    {
        if (Summoner.IsUnityNull()) 
            return;

        foreach (var collider in Physics.OverlapSphere(transform.position, Range))
        {
            var damage = explodeDamage;

            damage.sender = Summoner;
            damage.pushDirection = collider.transform.position - transform.position;
            damage.pushDirection.Normalize();
            damage.pushDirection *= PushForce;
            damage.pushDirection += transform.rotation * explodeDamage.pushDirection;

            var report = Damage.Deliver(collider.gameObject, damage);
            
            onDamageDeliveryReport?.Invoke(report);
        }

        if (!OnExplodePrefab.IsUnityNull())
        {
            var gameObject = Instantiate(OnExplodePrefab, transform.position, transform.rotation);

            gameObject.SetActive(true);

            Destroy(gameObject, 4);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}