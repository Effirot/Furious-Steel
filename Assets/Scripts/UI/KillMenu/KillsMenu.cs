using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class KillsMenu : NetworkBehaviour
{
    [SerializeField]
    private GameObject killPrefab;

    public override void OnDestroy()
    {
        base.OnDestroy();

        Damage.damageDeliveryPipeline -= OnDamageDelivered;
    }
    private void Start()
    {
        Damage.damageDeliveryPipeline += OnDamageDelivered;
    }

    private void OnDamageDelivered(DamageDeliveryReport report)
    {
        if (!IsServer)
            return;

        if (!report.isLethal) 
            return;
        if (report.target.IsUnityNull()) 
            return;

        Draw_ClientRpc(report);
    }

    [ClientRpc]
    private void Draw_ClientRpc(DamageDeliveryReport report)
    {
        var obj = Instantiate(killPrefab, transform);
        obj.GetComponent<KillsMenuElement>().Initialize(report);
        obj.SetActive(true);
    }
}
