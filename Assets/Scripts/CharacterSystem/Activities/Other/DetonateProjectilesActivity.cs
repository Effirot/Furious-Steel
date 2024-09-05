using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using UnityEngine;

public class DetonateProjectilesActivity : SyncedActivitySource<IAttackSource>
{
    public override IEnumerator Process()
    {
        DetonatableExplodeProjectile.Detonate(Source);

        yield break;
    }

    protected override void OnDestroy()
    {
        DetonatableExplodeProjectile.Detonate(Source);
    }
}
