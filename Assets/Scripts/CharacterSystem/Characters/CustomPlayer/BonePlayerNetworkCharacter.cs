using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BonePlayerNetworkCharacter : PlayerNetworkCharacter
{
    [Space]
    [Header("Splash attack")]
    [SerializeField]
    private Damage damage = new (30, null, 1, Vector3.up, Damage.Type.Unblockable);

    [SerializeField, Range(0, 3)]
    private float splashAttackDelay;

    [SerializeField, Range(0, 10)]
    private float splashAttackRange;

    private bool isAttacking = false; 

    public override bool Hit(Damage damage)
    {
        if (!isAttacking)
        {
            SplashAttack(transform.position);
        }

        return base.Hit(damage);
    }

    public override void DamageDelivered(DamageDeliveryReport report)
    {
        if (!isAttacking)
        {
            SplashAttack(transform.position);
        }

        base.DamageDelivered(report);
    }

    private async void SplashAttack(Vector3 position)
    {
        isAttacking = true;
        damage.sender = this;

        await UniTask.WaitForSeconds(splashAttackDelay);
        
        foreach (var target in Physics.OverlapSphere(position, splashAttackRange))
        {
            Damage.Deliver(target.gameObject, damage);
        }

        isAttacking = false;
    } 
}
