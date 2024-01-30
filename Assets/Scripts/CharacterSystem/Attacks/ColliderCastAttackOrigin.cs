using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class ColliderCastAttackOrigin : AttackOrigin
{

    [SerializeField, Range(0, 10)]
    public float BeforeAttackDelay;

    [SerializeField, Range(0, 10)]
    public float AfterAttackDelay;

    [SerializeField, SerializeReference, SubclassSelector]
    protected Caster[] casters;

    [SerializeField]
    private CinemachineImpulseSource impulseSource;

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

        Attack();
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
                caster?.CastColliderGizmos(transform);
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
                    impulseSource.GenerateImpulse(VecrtorToTarget * damage.Value / 90);
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
        public float Damage;
        public float PushForce;

        public abstract Collider[] CastCollider(Transform transform);
        public abstract void CastColliderGizmos(Transform transform);
    }

    [Serializable]
    public class BoxCaster : Caster
    {
        public Vector3 position;
        public Vector3 size;
        public Vector3 angle;

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
        public float radius;

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
}
