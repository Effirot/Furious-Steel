using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Unity.Burst.Intrinsics;

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

            while (IsPressed && IsPerforming)
            {
                currentAttackStatement = AttackTimingStatement.BeforeAttack;
                OnStartAttackEvent.Invoke();
                while (IsPressed && IsPerforming)
                {
                    yield return new WaitForSeconds(0.01f);

                    if (DisableMovingBeforeAttack)
                    {
                        Player.stunlock = 0.2f;
                    }

                    if(BeforeAttackDelay > charge)
                    {
                        charge = Mathf.Clamp(charge + 0.01f, 0, BeforeAttackDelay);
                        
                        OnChargeChanged.Invoke(charge);
                    }
                }

                currentAttackStatement = AttackTimingStatement.Attack;
                Execute(charge / BeforeAttackDelay);
                OnAttackEvent.Invoke();

                if (DisableMovingAfterAttack)
                {
                    Player.stunlock = AfterAttackDelay;
                }

                currentAttackStatement = AttackTimingStatement.AfterAttack;
                yield return new WaitForSeconds(AfterAttackDelay);
                OnEndAttackEvent.Invoke();

                EndAttack();
                currentAttackStatement = AttackTimingStatement.Waiting;

            }
            
            EndAttack();
        }
    }
}