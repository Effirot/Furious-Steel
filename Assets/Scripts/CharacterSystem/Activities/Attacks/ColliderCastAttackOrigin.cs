using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace CharacterSystem.Attacks
{
    public class ColliderCastAttackOrigin : DamageSource
    {
        [SerializeField]
        protected Vector3 RecieverPushDirection = Vector3.forward * 5;

        [Space]
        [Header("Attack")]
        [SerializeField, Range(0, 10)]
        public float BeforeAttackDelay;

        [SerializeField]
        public CharacterPermission beforeAttackPermissions = CharacterPermission.All;

        [SerializeField, Range(0, 10)]
        public float AfterAttackDelay;

        [SerializeField]
        public CharacterPermission afterAttackPermissions = CharacterPermission.All;


        [Space]
        [Header("Impulse")]
        [SerializeField, Range(0, 5)]
        private float impulseForce = 0.01f;

        [SerializeField]
        private CinemachineImpulseSource impulseSource;

        [Space]
        [Header("Animations")]
        [SerializeField]
        private string PlayAnimationName;
        
        [Space]
        [Header("Events")]
        [SerializeField]
        public UnityEvent OnStartAttackEvent = new();

        [SerializeField]
        public UnityEvent OnAttackEvent = new();


        [SerializeField]
        public UnityEvent OnEndAttackEvent = new();


        protected CharacterPermission characterPermissionsBuffer;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            OnHitEvent.AddListener((damage) => impulseSource?.GenerateImpulse(impulseForce));
        }

        protected override IEnumerator AttackProcessRoutine()
        {
            currentAttackStatement = AttackTimingStatement.BeforeAttack;
            
            Invoker.permissions = beforeAttackPermissions;

            OnStartAttackEvent.Invoke();
            yield return new WaitForSeconds(BeforeAttackDelay);

            currentAttackStatement = AttackTimingStatement.Attack;

            
            PlayAnimation();
            Invoker.Push(Invoker.transform.rotation * RecieverPushDirection);   
            Execute();
            OnAttackEvent.Invoke();

            Invoker.permissions = afterAttackPermissions;         

            currentAttackStatement = AttackTimingStatement.AfterAttack;
            yield return new WaitForSeconds(AfterAttackDelay);
            OnEndAttackEvent.Invoke();

            Invoker.permissions = CharacterPermission.All;
            currentAttackStatement = AttackTimingStatement.Waiting;
            EndAttack();
        }

        protected void PlayAnimation()
        {
            if (PlayAnimationName.Length > 0)
            {
                Invoker.animator.Play(PlayAnimationName);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Invoker != null)
            {
                DrawArrow(Invoker.transform.position, RecieverPushDirection, Color.cyan);
            }

            if (casters != null)
            {
                foreach(var caster in casters)
                {
                    if (caster != null)
                    {
                        if (caster.CastCollider(transform).Any())
                        {
                            Gizmos.color = Color.red;
                        }
                        else
                        {   
                            Gizmos.color = Color.yellow;
                        }

                        caster.CastColliderGizmos(transform);
                    }
                }
            }
        }

        public static void DrawArrow(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
        {
            Gizmos.color = color;
            Gizmos.DrawRay(pos, direction);
        
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
            Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
            Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
        }
    
    }
}