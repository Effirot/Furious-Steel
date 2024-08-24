
using CharacterSystem.DamageMath;
using Cysharp.Threading.Tasks;
using Mirror;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

namespace CharacterSystem.Interactions
{
    public class ExplodeProp : Prop
    {
        [Space]
        [SerializeField, Range(0, 30)]
        private float radius = 5;
        
        [SerializeField, Range(0, 3)]
        private float pushForce;

        [SerializeField]
        private Damage explodeDamage;

        [SerializeField]
        private GameObject OnExplodePrefab;

        public override bool Hit(Damage damage)
        {
            explodeDamage.senderID = damage.senderID;

            return base.Hit(damage);
        }

        public override void Throw(IThrower thrower)
        {
            base.Throw(thrower);

            explodeDamage.sender = thrower;
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Gizmos.DrawWireSphere(transform.position, radius);
        }

        public override void Kill(Damage damage)
        {
            Explode();
            
            base.Kill(damage);
        }

        public void Explode()
        {
            if (isServer)
            {
                Explode_ClientRpc();
            }

            if (!OnExplodePrefab.IsUnityNull())
            {
                var gameObject = Instantiate(OnExplodePrefab, transform.position, transform.rotation);

                gameObject.SetActive(true);

                gameObject.GetComponent<CinemachineImpulseSource>()?.GenerateImpulse();

                Destroy(gameObject, 4);
            }

            if (isServer)
            {
                foreach (var collider in Physics.OverlapSphere(transform.position, radius))
                {
                    var damage = explodeDamage;

                    damage.sender ??= thrower;
                    damage.pushDirection = collider.transform.position - transform.position;
                    damage.pushDirection.Normalize();
                    damage.pushDirection *= pushForce;

                    var originalPushDirection = transform.rotation * explodeDamage.pushDirection;
                    damage.pushDirection.y = Mathf.Max(damage.pushDirection.y, originalPushDirection.y);

                    var report = Damage.Deliver(collider.gameObject, damage);
                    
                    thrower?.DamageDelivered(report);
                }
            }
        }

        [ClientRpc]
        private void Explode_ClientRpc()
        {
            if (!isServer)
            {
                Explode();
            }
        }
    }
}