using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using UnityEngine;
using System;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Blocking;
using Unity.VisualScripting;
using UnityEngine.Events;
using Mirror;
using Cysharp.Threading.Tasks;



#if UNITY_EDITOR
using UnityEditor;
#endif


namespace CharacterSystem.PowerUps
{
    public interface IPowerUpActivator : 
        ISyncedActivitiesSource,
        IDamageSource,
        IDamagable
    {
        UnityEvent<PowerUp> onPowerUpChanged { get; } 

        int PowerUpId { get; set; }
        
        public PowerUp PowerUp {
            get => PowerUp.IdToPowerUpLink(PowerUpId);
            set => PowerUpId = PowerUp.PowerUpLinkToID(value);
        }
    }

    public class PowerUpHolder : SyncedActivitySource<IPowerUpActivator>
    {
        public async void Drop(PowerUp powerUp)
        {
            var powerupGameObject = Instantiate(Source.PowerUp.prefab, transform.position, Quaternion.identity);
        
            NetworkServer.Spawn(powerupGameObject);

            await UniTask.WaitForSeconds(10);

            if (!powerupGameObject.IsUnityNull())
            {
                NetworkServer.Destroy(powerupGameObject);
            }
        }
        
        public override IEnumerator Process()
        {
            if (Source.permissions.HasFlag(CharacterPermission.AllowPowerUps))
            {
                Source.PowerUp?.Activate(this);

                if (isServer)
                {
                    Source.PowerUp = null;
                }
            }

            yield break;
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (HasOverrides()) return;

            if (other.TryGetComponent<PowerUpContainer>(out var container) && isServer)
            {
                if (container.powerUp.IsOneshot)
                {
                    container.powerUp.Activate(this);
                    NetworkServer.Destroy(container.gameObject);
                }
                else
                {
                    if (Source.PowerUp == null)
                    {
                        Source.PowerUpId = container.Id;
                        
                        container.powerUp.OnPick(this);
                        NetworkServer.Destroy(container.gameObject);
                    }
                } 
            }
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(PowerUpHolder), true)]
        public class PowerUpHolder_Editor : Editor
        {
            new private PowerUpHolder target => base.target as PowerUpHolder;

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                GUILayout.Label(target.Source?.PowerUp?.GetType().Name ?? "None");
            }
        }

#endif
    }
}