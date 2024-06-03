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
    [Header("Splash attack")]
    [SerializeField, Range(0, 1)]
    private float healthDrainPercent = 0.05f;

    [SerializeField]
    private VisualEffect healthDrainEffect;

    public override void DamageDelivered(DamageDeliveryReport report)
    {
        if (report.isDelivered && !report.isBlocked)
        {
            Heal(new Damage(-report.damage.value * healthDrainPercent, null, 0, Vector3.zero, Damage.Type.Effect));

            if (report.target.gameObject.TryGetComponent<NetworkObject>(out var component))
            {
                SplashAttack_ClientRpc(component.NetworkObjectId);
            }
        }
    
        base.DamageDelivered(report);
    }

    [ClientRpc]
    private void SplashAttack_ClientRpc(ulong targetID)
    {
        var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;
        
        if (!dictionary.ContainsKey(targetID)) return;
        var target = dictionary[targetID];

        healthDrainEffect.SetVector3("SpawnPosition", target.transform.position + Vector3.up);
        healthDrainEffect.Play();
    }
}
