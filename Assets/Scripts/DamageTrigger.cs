using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using Mirror;
using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    [SerializeField]
    private string IgnoreTag;

    [SerializeField]
    private Damage damage;

    [SerializeField]
    private bool destroySelfOnDeliver = false;


    private void OnTriggerStay(Collider other)
    {
        if (other.tag != IgnoreTag)
        {
            using (var report = Damage.Deliver(other.gameObject, damage))
            {
                if (report.isDelivered && destroySelfOnDeliver && NetworkServer.active)
                {
                    NetworkServer.Destroy(gameObject);
                }
            }
        }
    }
}
