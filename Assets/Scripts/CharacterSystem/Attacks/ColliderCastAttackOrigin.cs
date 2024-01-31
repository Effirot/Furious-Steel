using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class ColliderCastAttackOrigin : AttackOrigin
{
    [Space]
    [Header("Delay")]
    [SerializeField, Range(0, 10)]
    public float BeforeAttackDelay;

    [SerializeField, Range(0, 10)]
    public float AfterAttackDelay;

    [SerializeField, SerializeReference, SubclassSelector]
    protected Caster[] casters;

    [Space]
    [Header("Impulse")]
    [SerializeField, Range(0, 5)]
    private float impulseForce = 0.01f;

    [SerializeField]
    private CinemachineImpulseSource impulseSource;

    [Space]
    [Header("Events")]
    [SerializeField]
    public UnityEvent OnStartAttackEvent = new();

    [SerializeField]
    public UnityEvent OnAttackEvent = new();

    [SerializeField]
    public UnityEvent OnEndAttackEvent = new();


    protected override IEnumerator AttackProcessRoutine()
    {
        OnStartAttackEvent.Invoke();
        yield return new WaitForSeconds(BeforeAttackDelay);

        ExecuteCasters();
        OnAttackEvent.Invoke();

        yield return new WaitForSeconds(AfterAttackDelay);
        OnEndAttackEvent.Invoke();

        EndAttack();
    }

    private void OnDrawGizmosSelected()
    {
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

    protected void ExecuteCasters(float Multiplayer = 1)
    {
        foreach (var cast in casters)
        {
            var damage = new Damage();
            damage.Value = cast.Damage * Multiplayer;

            foreach (var collider in cast.CastCollider(transform))
            {
                var VecrtorToTarget = collider.transform.position - transform.position;

                if (impulseSource != null)
                {
                    impulseSource.GenerateImpulse(VecrtorToTarget * damage.Value * impulseForce);
                }

                if (collider.gameObject.TryGetComponent<IDamagable>(out var damagable))
                {
                    damagable.SendDamage(damage);
                    damagable.Stunlock = Mathf.Max(damagable.Stunlock, damage.Value / 50);
                    
                    VisualEffect effect = damagable.OnHitEffect;

                    if (effect != null)
                    {
                        effect.SetVector3("Direction", VecrtorToTarget * 3);

                        effect.Play();
                    }
                }

                if (collider.gameObject.TryGetComponent<Rigidbody>(out var rigidbody))
                {                   
                    rigidbody.AddForce(VecrtorToTarget.normalized * cast.PushForce * Multiplayer);
                }

            }
        }

        OnAttackEvent.Invoke();
    }


    [Serializable]
    public abstract class Caster 
    {
        public float Damage = 10;
        public float PushForce = 100;

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
