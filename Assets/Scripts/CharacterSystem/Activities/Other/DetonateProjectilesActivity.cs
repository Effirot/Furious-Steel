using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using UnityEngine;

public class DetonateProjectilesActivity : SyncedActivitySource<IDamageSource>
{
    public override IEnumerator Process()
    {
        DetonatableExplodeProjectile.Detonate(Source);

        yield break;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        DetonatableExplodeProjectile.Detonate(Source);
    }
}
