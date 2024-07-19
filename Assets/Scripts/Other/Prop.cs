
using System;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Interactions;
using Mirror;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

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
        [field : SerializeField]
        public bool Locked { get; set; } = true;
        
        [field : SerializeField]
        public bool Undestroyable { get; set; } = false;

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
        
        [field : SerializeField, Range(0f, 1)]
        public float bouncness { get; set; } = 0.1f;
        
        [field : SerializeField, Range(0f, 1)]
        public float friction { get; set; } = 0.1f;
        
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
        

        public bool Heal(Damage damage)
        {
            return false;
        }
        public bool Hit(Damage damage)
        {
            health -= damage.value;

            onDamageRecieved?.Invoke(damage);

            Push(damage.pushDirection);

            return false;
        }

        public virtual void Kill(Damage damage)
        {
            NetworkServer.Destroy(gameObject);
            Destroy(gameObject);
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
            if (!this.thrower.IsUnityNull())
            {
                transform.position = thrower.pickPoint.position;
                transform.rotation = thrower.pickPoint.rotation;

                return;
            }

            if (Locked) return;

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
            if (velocity.magnitude > 0.4f)
            {
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
    
                    Push(-damage.pushDirection * bouncness);
                }
            }

            currentCollision = collision;
            
            var contact = collision.contacts.First().normal;
            var maxSeparation = collision.contacts.Max(contact => Mathf.Abs(contact.separation));
            transform.position += contact * maxSeparation;
        }
        protected virtual void OnCollisionStay(Collision collision)
        {
            currentCollision = collision;

            var contact = collision.contacts.First().normal;
            var maxSeparation = collision.contacts.Max(contact => Mathf.Abs(contact.separation));
            transform.position += contact * maxSeparation;
        }
        protected virtual void OnCollisionExit(Collision collision)
        {
            if (collision.contactCount <= 0)
            {
                currentCollision = null;
            }
        }

        private void CalculateVelocity()
        {
            var velocity = this.velocity;
            var timeScale = Time.fixedDeltaTime * LocalTimeScale * PhysicTimeScale;
            var interpolateValue = mass * timeScale;

            velocity = Vector3.Lerp(velocity, Physics.gravity, interpolateValue);

            
            if (currentCollision != null)
            {
                var vector = Vector3.zero;

                var contact = currentCollision.contacts.First();
                var angle = Quaternion.FromToRotation(contact.normal, Vector3.down);

                var deltaVelocity = angle * velocity;

                var bouncnessVelocity = -Mathf.Min(0, deltaVelocity.y * bouncness);
                deltaVelocity.y = Mathf.Max(0, deltaVelocity.y);

                deltaVelocity.x *= 1 - friction;
                deltaVelocity.z *= 1 - friction;
                
                velocity = Quaternion.Inverse(angle) * deltaVelocity;
            }

            if (velocity.magnitude <= 0.05f) 
            {
                Locked = true;
                velocity = Vector3.zero;
            }

            this.velocity = velocity;
        }
        private void Move()
        {
            var timeScale = LocalTimeScale * PhysicTimeScale * Time.fixedDeltaTime * 50;

            if (isServer)
            {            
                position = transform.position = (transform.position + (velocity * timeScale));
            }
            else
            {
                if (Vector3.Distance(position, transform.position) < 0.5f)
                {
                    var direction = position - transform.position;

                    transform.position = transform.position + direction + (velocity * timeScale);
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, position, 0.2f);
                }
            }
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

        public void OnSelect(IInteractor interactor)
        {
            
        }
        public void OnDeselect(IInteractor interactor)
        {
            
        }
        public void Interact(IInteractor interactor)
        {
            
        }
        public void Pick(IThrower thrower)
        {
            DamageOnHit.sender = this.thrower = thrower; 

            foreach (var collider in GetComponents<Collider>())
            {
                collider.enabled = false;
            }
        }
        public void Throw(IThrower thrower)
        {
            this.thrower = null; 
            
            foreach (var collider in GetComponents<Collider>())
            {
                collider.enabled = true;
            }
        }
    }
}