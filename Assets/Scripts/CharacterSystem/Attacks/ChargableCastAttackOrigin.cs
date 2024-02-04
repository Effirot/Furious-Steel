using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace CharacterSystem.Attacks
{
    public class ChargableColliderCastAttackOrigin : ColliderCastAttackOrigin
    {
        [SerializeField]
        public UnityEvent<float> OnChargeChanged = new();
        
        private float charge = 0;

        protected override IEnumerator AttackProcessRoutine()
        {
            OnStartAttackEvent.Invoke();

            while (IsPressed)
            {
                while (IsPressed)
                {
                    yield return new WaitForSeconds(0.01f);

                    if(BeforeAttackDelay > charge)
                    {
                        charge = Mathf.Clamp(charge + 0.01f, 0, BeforeAttackDelay);
                        
                        OnChargeChanged.Invoke(charge);
                    }
                }

                Execute(charge / BeforeAttackDelay);
                OnAttackEvent.Invoke();

                charge = 0;

                OnChargeChanged.Invoke(charge);

                yield return new WaitForSeconds(AfterAttackDelay);
                OnEndAttackEvent.Invoke();

            }
            
            EndAttack();
        }
    }
}