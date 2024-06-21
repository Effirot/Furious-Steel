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

    protected override void Start()
    {
        base.Start();

        detonateAll_Evenet.AddListener(Detonate_Event);
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();

        detonateAll_Evenet.RemoveListener(Detonate_Event);
    }

    private async void Detonate_Event(IDamageSource source)
    {
        detonateAll_Evenet.RemoveListener(Detonate_Event);

        if (System.Object.ReferenceEquals(source, Summoner))
        {
            OnAfterDetonateEvent.Invoke();
            
            var random = new System.Random((int)netId);

            await UniTask.WaitForSeconds(ExplodeAfter);
            await UniTask.WaitForSeconds(random.Next(0, 200) / 1000f);

            Explode();
            Kill();
        }
    }
}
