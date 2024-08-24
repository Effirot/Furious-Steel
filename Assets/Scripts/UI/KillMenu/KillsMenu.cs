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

    private void Start()
    {
        Damage.damageDeliveryPipeline += OnDamageDelivered;
    }
    private void OnDestroy()
    {
        Damage.damageDeliveryPipeline -= OnDamageDelivered;
    }

    private void OnDamageDelivered(DamageDeliveryReport report)
    {
        if (!isServer || !report.isLethal || report.target.IsUnityNull()) 
            return;

        Draw(report.damage.sender?.gameObject.name ?? "", report.target?.gameObject.name ?? "");
    }

    [ClientRpc]
    private void Draw(string KillerName, string KilledName)
    {
        var obj = Instantiate(killPrefab, transform);
        obj.GetComponent<KillsMenuElement>().Initialize(KillerName, KilledName);
        obj.SetActive(true);
    }
}
