using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;

public class ExplodePowerUp : PowerUp
{
    public override GameObject prefab => throw new System.NotImplementedException();


    public override void Activate(PowerUpHolder holder)
    {
        if (holder.IsServer)
        {
            foreach (var collider in Physics.OverlapSphere(holder.transform.position, 4))
            {
                if (collider.TryGetComponent<IDamagable>(out var damagable))
                {
                    damagable.Hit(new Damage()
                    {
                        Value = 60,
                        Stunlock = 1,
                        PushForce = 400,
                        Sender = holder.Character
                    });
                }
            }
        }
    }

    public override void OnPick(PowerUpHolder holder)
    {

    }
}