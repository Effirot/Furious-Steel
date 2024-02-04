using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CharacterSystem.Attacks;
using UnityEngine;

namespace CharacterSystem.Objects.AI
{
    public class DefaultEnemyCharacter : AINetworkCharacterMovement
    {
        protected override Vector3 targetPosition => followPoint;

        [field : SerializeField]
        public Transform followTarget { get; set; } = null;

        [SerializeField]
        private Vector3 followPoint;

        [SerializeField, Range(1, 10)]
        private float SearchingRange = 5;

        [SerializeField, Range(1, 10)]
        private float AttackRange = 5;

        [SerializeField]
        private ChargableColliderCastAttackOrigin attack;

        private Coroutine AttackProcess = null;

        public void PushForward(float force)
        {
            rigidbody.AddForce(transform.forward * force);
        }

        protected override void AITick()
        {
            if (AttackProcess == null)
            {
                ResearchPlayer();

                if (followTarget != null)
                {
                    followPoint = followTarget.position;

                    PrepareAttack();
                }
            } 

        }

        private void ResearchPlayer()
        {
            var collisions = Physics.OverlapSphere(transform.position, SearchingRange, LayerMask.GetMask("Character")).Where(collider => collider.gameObject.tag == "Player");
        
            float distance = 10000;
            Transform target = null;

            foreach (var collision in collisions)
            {
                var NewDisance = Vector3.Distance(collision.transform.position, transform.position);

                if (NewDisance < distance)
                {
                    target = collision.transform;
                    distance = NewDisance;
                }
            }

            followTarget = target;

            FollowPath = followTarget != null;
        }

        private void PrepareAttack()
        {
            if (Vector3.Distance(followTarget.position, transform.position) < AttackRange)
            {
                AttackProcess = StartCoroutine(StartAttack());
            }
        }

        private IEnumerator StartAttack()
        {
            SetMovementVector(Vector3.zero);

            FollowPath = false;
            attack.IsPressed = true;

            yield return new WaitForSeconds(attack.BeforeAttackDelay);

            attack.IsPressed = false;

            yield return new WaitForSeconds(attack.AfterAttackDelay);

            AttackProcess = null;
        }
    }
}