

using UnityEngine;
using CharacterSystem.DamageMath;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using System.Runtime.CompilerServices;
using CharacterSystem.Objects;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks;
using TMPro;
using Cinemachine;

public class ExplodeMagicTrinketUltimate : UltimateDamageSource
{
    [SerializeField]
    private GameObject ExplodeEffectPrefab;

    [SerializeField]
    private BurnEffect burnEffect = new();

    [SerializeField]
    private Damage explodeDamage = new();

    private List<BurnEffect> burnEffects = new();

    private void OnDamageDelivered_event(DamageDeliveryReport report)
    {
        if (report.damage.type != CharacterSystem.DamageMath.Damage.Type.Effect && !report.target.IsUnityNull())
        {
            var deliveryReport = Damage.Deliver(report.target, new Damage(0, Invoker, 0, Vector3.zero, Damage.Type.Effect, burnEffect));
            
            var collection = deliveryReport?.RecievedEffects?.Where(effect => effect is BurnEffect and not null).Select(effect => (BurnEffect)effect);

            if (collection != null && collection.Any())
            {
                burnEffects.AddRange(collection);
            }
        }
    }

    public override void Play()
    {
        if (chargeValue.Value >= chargeValue.MaxValue && 
            Invoker.permissions.HasFlag(CharacterPermission.AllowAttacking) &&
            !Invoker.isStunned && 
            IsPerforming &&
            !IsInProcess)
        {
            ExplodeAll();

            base.PlayForced();
        }
    }

    private async void ExplodeAll()
    {
        if (IsServer)
        {            
            var effects = burnEffects.ToArray();
            
            for (int i = 0; i < effects.Length; i++)
            {
                effects.TrySwap(i, Random.Range(0, effects.Length - 1), out _);
            }

            foreach (var effect in effects)
            {
                effect.time = 100;
            }

            foreach (var effect in effects)
            {
                if (effect.effectsHolder.IsUnityNull() || !effect.IsValid)
                    continue;

                Explode_ClientRpc(effect.effectsHolder.transform.position);
               
                if (!IsClient)
                {
                    Explode_Internal(effect.effectsHolder.transform.position);
                }

                effect.time = -1;
                
                foreach (var collider in Physics.OverlapSphere(effect.effectsHolder.transform.position, 5))
                {
                    var damage = explodeDamage; 

                    var VectorToTarget = effect.effectsHolder.transform.position - collider.transform.position;
                    VectorToTarget.Normalize();
                    damage.pushDirection = VectorToTarget  + transform.rotation * damage.pushDirection;
                    damage.sender = Invoker;

                    Damage.Deliver(collider.gameObject, damage);
                }

                chargeValue.Value = 0;

                await UniTask.WaitForSeconds(0.2f);
            }
        }

        burnEffects.Clear();
    }

    [ClientRpc]
    private void Explode_ClientRpc(Vector3 position)
    {
        Explode_Internal(position);
    }
    private void Explode_Internal(Vector3 position)
    {
        var gameObject = Instantiate(ExplodeEffectPrefab, position, Quaternion.identity);
        gameObject.SetActive(true);
        Destroy(gameObject, 5);

        gameObject.GetComponent<CinemachineImpulseSource>()?.GenerateImpulse();   
    }    

    protected override void Start()
    {
        base.Start();

        Invoker.onDamageDelivered += OnDamageDelivered_event;
    }
}