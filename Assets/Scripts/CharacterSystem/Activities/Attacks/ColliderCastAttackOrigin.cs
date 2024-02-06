using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace CharacterSystem.Attacks
{
    public class ColliderCastAttackOrigin : AttackOrigin
    {
        [SerializeField]
        private Vector3 RecieverPushDirection = Vector3.forward * 5;

        [Space]
        [Header("Attack")]
        [SerializeField, Range(0, 10)]
        public float BeforeAttackDelay;

        [SerializeField]
        private bool DisableMovingBeforeAttack = true;

        [SerializeField, Range(0, 10)]
        public float AfterAttackDelay;

        [SerializeField]
        private bool DisableMovingAfterAttack = true;

        [SerializeField, SerializeReference, SubclassSelector]
        protected Caster[] casters;

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
        public UnityEvent<Damage> OnHitEvent = new();

        [SerializeField]
        public UnityEvent OnEndAttackEvent = new();



        protected override IEnumerator AttackProcessRoutine()
        {
            if (DisableMovingBeforeAttack)
            {
                Player.stunlock = BeforeAttackDelay;
            }

            OnStartAttackEvent.Invoke();
            yield return new WaitForSeconds(BeforeAttackDelay);

            Execute();
            OnAttackEvent.Invoke();

            if (DisableMovingAfterAttack)
            {
                Player.stunlock = AfterAttackDelay;
            }

            yield return new WaitForSeconds(AfterAttackDelay);
            OnEndAttackEvent.Invoke();

            EndAttack();
        }

        private void OnDrawGizmosSelected()
        {
            if (Player != null)
            {
                DrawArrow(Player.transform.position, RecieverPushDirection, Color.cyan);
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
    

        protected void Execute(float Multiplayer = 1)
        {
            if (Player.isStunned) return;

            if (PlayAnimationName.Length > 0)
            {
                Player.animator.Play(PlayAnimationName);
            }

            Player.Push(Player.transform.rotation * RecieverPushDirection * Multiplayer);           

            foreach (var cast in casters)
            {
                foreach (var collider in cast.CastCollider(transform))
                {
                    var damage = cast.damage;
                    damage *= Multiplayer;
                    damage.Sender = Player;


                    var VecrtorToTarget = collider.transform.position - transform.position;

                    if (impulseSource != null)
                    {
                        impulseSource.GenerateImpulse(VecrtorToTarget * damage.Value * impulseForce);
                    }

                    if (collider.gameObject.TryGetComponent<IDamagable>(out var damagable))
                    {
                        damagable.Hit(damage);
                    }

                    OnHitEvent.Invoke(damage);
                }
            }
        }


        [Serializable]
        public abstract class Caster 
        {
            public Damage damage;

            public abstract Collider[] CastCollider(Transform transform);
            public abstract void CastColliderGizmos(Transform transform);
        }

        [Serializable]
        public class BoxCaster : Caster
        {
            public Vector3 position = Vector3.zero;
            public Vector3 size = Vector3.one;
            public Vector3 angle = Vector3.zero;

            public override Collider[] CastCollider(Transform transform)
            {
                return Physics.OverlapBox(transform.position + (transform.rotation * position), size, Quaternion.Euler(angle + transform.eulerAngles));
            }

            public override void CastColliderGizmos(Transform transform)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position + (transform.rotation * position), Quaternion.Euler(transform.eulerAngles + angle), Vector3.one);

                Gizmos.DrawWireCube(Vector3.zero, size * 2);
            }
        }
        [Serializable]
        public class SphereCaster : Caster
        {
            public Vector3 position;
            public float radius = 1;

            public override Collider[] CastCollider(Transform transform)
            {
                return Physics.OverlapSphere(transform.position + (transform.rotation * position), radius);
            }

            public override void CastColliderGizmos(Transform transform)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position + (transform.rotation * position), transform.rotation, Vector3.one);

                Gizmos.DrawWireSphere(Vector3.zero, radius);
            }
        }
        [Serializable]
        public class RaycastCaster : Caster
        {
            public Vector3 origin;
            public Vector3 direction;
            public float maxDistance;

            public override Collider[] CastCollider(Transform transform)
            {
                if (Physics.Raycast((transform.rotation * origin) + transform.position, transform.rotation * direction, out var hit, maxDistance))
                {
                    return new Collider[] { hit.collider };
                }
                else
                {
                    return new Collider[0];
                }
            }

            public override void CastColliderGizmos(Transform transform)
            {
                Gizmos.DrawRay((transform.rotation * origin) + transform.position, (transform.rotation * direction.normalized) * maxDistance);
            }
        }
    }
}