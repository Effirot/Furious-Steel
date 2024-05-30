using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CharacterSystem.PowerUps
{
    public sealed class BagPowerUpHolder : PowerUpHolder
    {
        protected override void OnTriggerStay(Collider other)
        {
            if (HasOverrides()) return;

            if (other.TryGetComponent<PowerUpContainer>(out var container) && IsServer)
            {
                if (container.powerUp.IsOneshot && Invoker.PowerUp == null)
                {
                    Invoker.PowerUp = container.powerUp;
                    
                    container.powerUp.OnPick(this);
                    container.NetworkObject.Despawn();
                }
            }
        }
    }
}