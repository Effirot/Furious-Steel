using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using UnityEngine;

public class KillTrigger : MonoBehaviour
{
    [SerializeField]
    private string IgnoreTag;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != IgnoreTag)
        {
            if (other.gameObject.TryGetComponent<IDamagable>(out var damagable))
            {
                damagable.Kill();
            }
        }
    }
}
