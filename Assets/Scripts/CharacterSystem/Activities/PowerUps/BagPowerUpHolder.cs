using UnityEngine;
using Mirror;



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

            if (other.TryGetComponent<PowerUpContainer>(out var container) && isServer)
            {
                if (container.powerUp.IsOneshot && Source.PowerUp == null)
                {
                    Source.PowerUp = container.powerUp;
                    
                    container.powerUp.OnPick(this);
                    NetworkServer.Destroy(container.gameObject);
                }
            }
        }
    }
}