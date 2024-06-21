using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using CharacterSystem.PowerUps;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class ToxicPowerUp : PowerUp
{
    private static GameObject projectilePrefab = Resources.Load<GameObject>("PowerUps/Prefabs/ToxicPowerUpProjectile");
    
    public override void Activate(PowerUpHolder holder)
    {
        if (holder.isServer)
        {
            var projectileEffectObject = GameObject.Instantiate(projectilePrefab, holder.transform.position, holder.transform.rotation);

            IDamageSource damageSource = holder.Source;

            if (projectileEffectObject.TryGetComponent<Projectile>(out var projectile))
            {
                if (damageSource != null)
                {
                    projectile.Initialize(holder.transform.forward, damageSource, damageSource.DamageDelivered);
                }
                else
                {
                    projectile.Initialize(holder.transform.forward, null);
                }
            }
        }
    }
}