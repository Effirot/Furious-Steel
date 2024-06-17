using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

public class DetonatableExplodeProjectile : ExplodeProjectile
{
    public float ExplodeAfter = 1;
    
    public UnityEvent OnAfterDetonateEvent = new();

    public static void Detonate(IDamageSource source)
    {
        detonateAll_Evenet.Invoke(source);
    }

    private static UnityEvent<IDamageSource> detonateAll_Evenet = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        detonateAll_Evenet.AddListener(Detonate_Event);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        detonateAll_Evenet.RemoveListener(Detonate_Event);
    }

    private async void Detonate_Event(IDamageSource source)
    {
        if (System.Object.ReferenceEquals(source, Summoner))
        {
            OnAfterDetonateEvent.Invoke();

            await UniTask.WaitForSeconds(ExplodeAfter);
            await UniTask.WaitForSeconds(UnityEngine.Random.Range(0, 0.2f));

            Kill();
        }
    }
}
