using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class BonePlayerNetworkCharacter : PlayerNetworkCharacter
{
    [Space]
    [Header("Heath draining")]
    [SerializeField, Range(0, 1)]
    private float healthDrainPercent = 0.05f;

    [SerializeField, Range(0, 75)]
    private float healtPerBlock = 15f;

    [SerializeField, Range(0, 5)]
    private float healReducingByRangeHits = 3f;
    
    [SerializeField, Range(0, 5)]
    private float healReducingByBotHits = 2f;

    [SerializeField]
    private VisualEffect healthDrainEffect;

    public override void DamageDelivered(DamageDeliveryReport report)
    {
        if (report.isDelivered && !report.isBlocked && report.damage.type is not Damage.Type.Effect)
        {
            if (report.isLethal)
            {
                Heal(new Damage(maxHealth, null, 0, Vector3.zero, Damage.Type.Effect));
            }
            else
            {
                var comboModificator = 1 + Combo * 0.01f; 
                var newDrainPercent = healthDrainPercent + comboModificator;

                if (report.damage.type is Damage.Type.Balistic or Damage.Type.Magical or Damage.Type.Unblockable)
                {
                    newDrainPercent /= healReducingByRangeHits;
                }

                if (report.target is not PlayerNetworkCharacter)
                {
                    newDrainPercent /= healReducingByBotHits;
                }

                Heal(new Damage(-report.damage.value * newDrainPercent, null, 0, Vector3.zero, Damage.Type.Effect));
            }

            if (report.target.gameObject.TryGetComponent<NetworkObject>(out var component))
            {
                HealthDrain_ClientRpc(component.NetworkObjectId);
            }
        }
    
        base.DamageDelivered(report);
    }
    public override bool Hit(Damage damage)
    {
        var result = base.Hit(damage);

        if (result)
        {
            result = result || Heal(new Damage(healtPerBlock, null, 0, Vector3.zero, Damage.Type.Effect));
        }

        return result;
    }


    [ClientRpc]
    private void HealthDrain_ClientRpc(ulong targetID)
    {
        var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

        if (!dictionary.ContainsKey(targetID)) return;
        var target = dictionary[targetID];

        healthDrainEffect.SetVector3("SpawnPosition", target.transform.position + Vector3.up);
        healthDrainEffect.Play();
    }
}
