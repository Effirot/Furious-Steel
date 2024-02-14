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
            characterPermissionsBuffer = Invoker.permissions;

            while (IsPressed && IsPerforming)
            {
                currentAttackStatement = AttackTimingStatement.BeforeAttack;
                OnStartAttackEvent.Invoke();
                while (IsPressed && IsPerforming)
                {
                    yield return new WaitForSeconds(0.01f);

                    Invoker.permissions = beforeAttackPermissions;

                    if(BeforeAttackDelay > charge)
                    {
                        charge = Mathf.Clamp(charge + 0.01f, 0, BeforeAttackDelay);
                        
                        OnChargeChanged.Invoke(charge);
                    }
                }

                currentAttackStatement = AttackTimingStatement.Attack;

                    
                PlayAnimation();
                Invoker.Push(Invoker.transform.rotation * RecieverPushDirection);  
                Execute(charge / BeforeAttackDelay);
                OnAttackEvent.Invoke();

                Invoker.permissions = afterAttackPermissions;

                currentAttackStatement = AttackTimingStatement.AfterAttack;
                yield return new WaitForSeconds(AfterAttackDelay);
                OnEndAttackEvent.Invoke();

                EndAttack();
                currentAttackStatement = AttackTimingStatement.Waiting;
            }
            
            Invoker.permissions = characterPermissionsBuffer;

            EndAttack();
        }
    }
}