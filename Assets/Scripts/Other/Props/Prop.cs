
using System;
using System.Collections;
using System.Linq;
using System.Xml.Schema;
using CharacterSystem.DamageMath;
using CharacterSystem.Interactions;
using Mirror;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public class Prop : NetworkBehaviour,
        IDamagable,
        IPhysicObject,
        ITimeScalable,
        IThrowable,
        IInteractable
    {
        [SerializeField, SyncVar]
        public bool Locked = true;
        
        [SerializeField]
        public bool Undestroyable = false;

        [field : SerializeField]
        public float maxHealth { get; set; }
        
        
        [SerializeField, SyncVar]
        public float health;
        [SerializeField, SyncVar]
        public Vector3 velocity;
        [SerializeField, SyncVar]
        public Vector3 position;



        [SerializeField]
        private Damage DamageOnHit = new();
        
        [SerializeField]
        public Damage selfDamageOnHit = new();


        [field : Space]
        [field : Header("Physics parameters")]
        [field : SerializeField, Range(0.01f, 10)]
        public float mass { get; set; } = 1;
                        
        [Header("Visual")]
        [SerializeField]
        private VisualEffect onHitEffect;

        [SerializeField]
        private AudioSource onHitSound;


        public float stunlock { get => 0; set { } }
        public Damage lastRecievedDamage { get; set; }

        public float PhysicTimeScale { get; set; } = 1;
        public float GravityScale { get; set; } = 1;
        public float LocalTimeScale { get; set; } = 1;

        public Team team => null;

        protected Rigidbody rb { get; private set; }

        float IDamagable.health { get => health; set => health = value; }
        Vector3 IPhysicObject.velocity { get => velocity; set => velocity = value; }


        public event Action<Damage> onDamageRecieved;
        
        private Collision currentCollision = null;
        
        protected IThrower thrower = null;
        

        public virtual IEnumerator Interact(IInteractor interactor)
        {
            yield break;
        }
        
        public virtual bool IsInteractionAllowed(IInteractor interactor) => true;
        public virtual void Pick(IThrower thrower)
        {
            DamageOnHit.sender = this.thrower = thrower; 

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            Locked = true;
            currentCollision = null;
        }
        public virtual void Throw(IThrower thrower)
        {
            foreach (var collider in GetComponents<Collider>())
            {
                collider.enabled = true;
            }

            Locked = false;
        }

        public virtual bool Heal(ref Damage damage)
        {
            return false;
        }
        public virtual bool Hit(ref Damage damage)
        {
            if (!Undestroyable)
            {
                health -= damage.value;
            }

            if (!onHitEffect.IsUnityNull())
            {
                onHitEffect.SetVector3("Direction", damage.pushDirection);
                onHitEffect.Play();
            }

            if (!onHitSound.IsUnityNull())
            {
                onHitSound.Play();
            }

            onDamageRecieved?.Invoke(damage);

            Push(damage.pushDirection);

            return false;
        }

        public virtual void Kill(Damage damage)
        {
            if (isServer)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        public void Push(Vector3 direction)
        {
            velocity = direction / mass;
            Locked = false;
        }

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.useGravity = false;
            
            health = maxHealth;
        }
        protected virtual void FixedUpdate()
        {   
            if (isServer)
            {
                position = transform.position;
            }
            else
            {
                SyncPosition();
            }

            if (Locked) {
                velocity = Vector3.zero;
                return;
            }
            

            if (NetworkManager.singleton?.isNetworkActive ?? false)
            {
                CalculateVelocity();

                Move();
                Rotate();
            }
        }
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;

            if (currentCollision != null)
            {
                foreach (var contact in currentCollision.contacts)
                {
                    Gizmos.DrawWireSphere(contact.point, 0.03f);
                    Gizmos.DrawRay(contact.point, contact.normal * contact.separation);
                }
            }

            Gizmos.DrawRay(transform.position, velocity);
        }
        

        protected virtual void OnCollisionEnter(Collision collision)
        {
            currentCollision = collision;
            var contact = collision.contacts.First();
         
            if (Locked) return;

            if (velocity.magnitude > 0.4f)
            {
                DeliverDamage(collision);
            }

            CalculateVelocity(collision);

            thrower = null;
            
            var maxSeparation = collision.contacts.Max(contact => Mathf.Abs(contact.separation));
            transform.position += contact.normal * maxSeparation;
        }
        protected virtual void OnCollisionStay(Collision collision)
        {

            currentCollision = collision;

            if (Locked) return;

            CalculateVelocity(collision);

            var contact = collision.contacts.First().normal;
            var maxSeparation = collision.contacts.Max(contact => Mathf.Abs(contact.separation));
            transform.position += contact * maxSeparation;
        }
        protected virtual void OnCollisionExit(Collision collision)
        {
            if (collision.contactCount <= 0)
            {
                currentCollision = null;
                Locked = false;
            }
        }

        private void CalculateVelocity()
        {
            var velocity = this.velocity;
            var timeScale = Time.fixedDeltaTime * LocalTimeScale * PhysicTimeScale;

            velocity = Vector3.Lerp(velocity, Physics.gravity, timeScale / mass / 2);

            if (velocity.magnitude <= Physics.sleepThreshold) 
            {
                Locked = true;
                velocity = Vector3.zero;
            }

            this.velocity = velocity;
        }
        private void Move()
        {
            var timeScale = LocalTimeScale * PhysicTimeScale * Time.fixedDeltaTime * 50;
            var direction = velocity * timeScale;

            if (Physics.Raycast(transform.position, direction, out var hit, direction.magnitude))
            {
                direction = direction.normalized * hit.distance;
            }
          
            transform.position = transform.position + direction;
        }
        private void Rotate()
        {
            var checkVelocity = velocity;
            checkVelocity.y = 0;

            if (currentCollision != null && currentCollision.contactCount > 0)
            {
                transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
            }
            else
            {
                if (checkVelocity.magnitude > 0.1f)
                {
                    transform.localEulerAngles = new Vector3(0, Quaternion.LookRotation(checkVelocity).eulerAngles.y, 0); 
                }
            }
        }
    
        private void CalculateVelocity(Collision collision)
        {
            var timeScale = Time.fixedDeltaTime * LocalTimeScale * PhysicTimeScale;
            var vector = Vector3.zero;

            var contact = currentCollision.contacts.First();
            var angle = Quaternion.FromToRotation(contact.normal, Vector3.down);

            var deltaVelocity = angle * velocity;

            var bounciness = contact.thisCollider.material.bounciness + contact.otherCollider.material.bounciness;
            bounciness /= 2;

            deltaVelocity.y = -Mathf.Min(0, deltaVelocity.y) * bounciness;
            
            deltaVelocity.x = Mathf.MoveTowards(deltaVelocity.x, 0, (contact.thisCollider.material.dynamicFriction + contact.otherCollider.material.dynamicFriction) / 5f * timeScale);
            deltaVelocity.z = Mathf.MoveTowards(deltaVelocity.z, 0, (contact.thisCollider.material.dynamicFriction + contact.otherCollider.material.dynamicFriction) / 5f * timeScale);

            velocity = Quaternion.Inverse(angle) * deltaVelocity;
        }
        private void DeliverDamage(Collision collision)
        {
            var contact = collision.contacts.First();

            var damage = DamageOnHit;
            damage.pushDirection = velocity.normalized * damage.pushDirection.magnitude;
            
            var report = Damage.Deliver(collision.gameObject, damage);

            if (damage.sender != null)
            {
                damage.sender.DamageDelivered(report);
            }

            if (report.isDelivered)
            {
                Damage.Deliver(this, selfDamageOnHit);

                var bounciness = contact.thisCollider.material.bounciness + contact.otherCollider.material.bounciness;

                Push(-damage.pushDirection * bounciness / 2);
            }
        }

        private void SyncPosition()
        {
            var timeScale = LocalTimeScale * PhysicTimeScale * Time.fixedDeltaTime;

            transform.position = Vector3.Lerp(transform.position, position, 18f * timeScale);

            if (Vector3.Distance(position, transform.position) > 0.5f)
            {
                transform.position = position;
            }
        }
    }
}