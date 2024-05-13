

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


    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        Explode();
    }

    private void Explode()
    {
        if (!IsOwner && Summoner.IsUnityNull()) 
            return;

        foreach (var collider in Physics.OverlapSphere(transform.position, Range))
        {
            explodeDamage.sender = Summoner;
            explodeDamage.pushDirection = collider.transform.position - transform.position + Vector3.up;
            explodeDamage.pushDirection.Normalize();
            explodeDamage.pushDirection *= PushForce;

            Summoner.DamageDelivered(Damage.Deliver(collider.gameObject, explodeDamage));
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}