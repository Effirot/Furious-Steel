using System.Collections;
using System.Linq;
using CharacterSystem.DamageMath;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace CharacterSystem.Interactions
{
    public class BombExplodeProp : ExplodeProp
    {
        [SerializeField]
        private float LitTimeout = 5;

        [SerializeField]
        private UnityEvent OnStartLit = new();
        [SerializeField]
        private UnityEvent OnStopLit = new();

        private Coroutine litProcess;

        public override IEnumerator Interact(IInteractor interactor) 
        {
            LightItUp();

            yield break;
        }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                PutOutTheFuse();
            }
        }

        public override bool Hit(ref Damage damage)
        {
            if (damage.args.Contains(Damage.DamageArgument.COLD))
            {
                PutOutTheFuse();
            }

            return base.Hit(ref damage);
        }

        private void LightItUp()
        {
            if (litProcess.IsUnityNull())
            {
                OnStartLit.Invoke();
                
                if (isServer)
                {
                    LightItUp_ClientRpc();
                }

                litProcess = StartCoroutine(LitWickProcess());
            }
        }
        private void PutOutTheFuse()
        {
            OnStopLit.Invoke();

            if (isServer)
            {
                PutOutTheFuse_ClientRpc();
            }

            if (!litProcess.IsUnityNull())
            {
                StopCoroutine(litProcess);
            }

            litProcess = null;
        }

        [ClientRpc]
        private void LightItUp_ClientRpc()
        {
            if (!isServer)
            {
                LightItUp();
            }
        }
        [ClientRpc]
        private void PutOutTheFuse_ClientRpc()
        {
            if (!isServer)
            {
                PutOutTheFuse();
            }
        }

        public override void Kill(Damage damage)
        {
            Explode();
            base.Kill(default);
        }

        private IEnumerator LitWickProcess()
        {
            yield return new WaitForSeconds(LitTimeout);

            Explode();
            Kill(default);
        }
    }
}