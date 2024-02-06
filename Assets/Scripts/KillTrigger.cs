using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using UnityEngine;

public class KillTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<IDamagable>(out var damagable))
        {
            damagable.Kill();
        }
    }
}
