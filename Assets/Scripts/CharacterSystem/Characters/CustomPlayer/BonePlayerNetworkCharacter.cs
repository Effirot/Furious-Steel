using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class BonePlayerNetworkCharacter : PlayerNetworkCharacter
{
    [Space]
    [Header("Splash attack")]
    [SerializeField]
    private Damage damage = new (30, null, 1, Vector3.up, Damage.Type.Unblockable);

    [SerializeField, Range(0, 300)]
    private float splashAttackRecievedDamage = 50;

    [SerializeField, Range(0, 10)]
    private float splashAttackRange;

    [Space]
    [SerializeField]
    public UnityEvent OnSplashDamageCast = new();

    private bool isAttacking = false; 
    private float recievedDamage = 0; 

    // public override bool Hit(Damage damage)
    // {
    //     if (!isAttacking)
    //     {
    //         recievedDamage += damage.value;

    //         SplashAttack();
    //     }

    //     return base.Hit(damage);
    // }
    public override void DamageDelivered(DamageDeliveryReport report)
    {
        recievedDamage += report.damage.value;

        if (!isAttacking && IsServer && recievedDamage >= splashAttackRecievedDamage)
        {

            SplashAttack_ClientRpc();

            if (!IsClient)
            {
                SplashAttack();
            }
        }

        base.DamageDelivered(report);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, splashAttackRange);
    }

    private void SplashAttack()
    {
        recievedDamage = 0;
        isAttacking = true;
        damage.sender = this;
        
        if (this == null || !IsSpawned) return;

        OnSplashDamageCast.Invoke();
        foreach (var target in Physics.OverlapSphere(transform.position, splashAttackRange))
        {
            var vector = target.transform.position - transform.position;
            if (vector.magnitude > 0)
            {
                damage.pushDirection = Quaternion.LookRotation(target.transform.position - transform.position) * damage.pushDirection;
            }
            else
            {
                damage.pushDirection = Vector3.zero;
            }


            Damage.Deliver(target.gameObject, damage);
        }

        isAttacking = false;
    } 
    [ClientRpc]
    private void SplashAttack_ClientRpc()
    {
        SplashAttack();
    }
}
