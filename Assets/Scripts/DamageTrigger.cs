using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    [SerializeField]
    private string IgnoreTag;

    [SerializeField]
    private Damage damage;

    private void OnTriggerStay(Collider other)
    {
        if (other.tag != IgnoreTag)
        {
            using (var report = Damage.Deliver(other.gameObject, damage))
            {
                
            }
        }
    }
}
