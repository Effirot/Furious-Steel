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
    public GameObject SplashHitPrefab;

    private float recievedDamage = 0;

    public override void DamageDelivered(DamageDeliveryReport report)
    {
        recievedDamage += report.damage.value;

        if (report.isDelivered && IsServer && recievedDamage >= splashAttackRecievedDamage)
        {
            if (report.target.gameObject.TryGetComponent<NetworkObject>(out var component))
            {
                var ID = component.NetworkObjectId;

                SplashAttack_ClientRpc(ID);

                if (!IsClient)
                {
                    SplashAttack(ID);
                }
            }
        }

        base.DamageDelivered(report);
    }

    private async void SplashAttack(ulong targetID)
    {

        var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;
        
        if (!dictionary.ContainsKey(targetID)) return;
        var target = dictionary[targetID];


        await UniTask.WaitForSeconds(0.3f); 
                
        if (target == null || this == null || !IsSpawned) return;

        recievedDamage = 0;
        damage.sender = this;

        var vector = target.transform.position - transform.position;
        Quaternion LookRotation = Quaternion.identity;
        if (vector.magnitude > 0)
        {
            LookRotation = Quaternion.LookRotation(target.transform.position - transform.position);
        }
        if (LookRotation != Quaternion.identity)
        {
            damage.pushDirection = LookRotation * damage.pushDirection;
        }

        if (SplashHitPrefab)
        {
            var effectObject = Instantiate(SplashHitPrefab, target.transform.position, LookRotation);
            effectObject.SetActive(true);

            Destroy(effectObject, 5);
        }

        Damage.Deliver(target.gameObject, damage);
    } 
    [ClientRpc]
    private void SplashAttack_ClientRpc(ulong targetID)
    {
        SplashAttack(targetID);
    }
}
