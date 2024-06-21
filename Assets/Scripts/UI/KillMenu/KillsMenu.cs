using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class KillsMenu : NetworkBehaviour
{
    [SerializeField]
    private GameObject killPrefab;

    private void OnDestroy()
    {
        Damage.damageDeliveryPipeline -= OnDamageDelivered;
    }
    private void Start()
    {
        Damage.damageDeliveryPipeline += OnDamageDelivered;
    }

    private void OnDamageDelivered(DamageDeliveryReport report)
    {
        if (!isServer)
            return;

        if (!report.isLethal) 
            return;
        if (report.target.IsUnityNull()) 
            return;

        Draw(report);
    }

    [Command(requiresAuthority = false)]
    private void Draw(DamageDeliveryReport report)
    {
        var obj = Instantiate(killPrefab, transform);
        obj.GetComponent<KillsMenuElement>().Initialize(report);
        obj.SetActive(true);
    }
}
