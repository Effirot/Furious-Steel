using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CharacterSystem.Objects.AI
{
    public abstract class AINetworkCharacterMovement : NetworkCharacter
    {
        [SerializeField]
        public bool FollowPath;
        
        private NavMeshPath path;

        protected abstract Vector3 targetPosition { get; }


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                StartCoroutine(AITickProcess());
            }
        }

        protected override void Awake()
        {
            base.Awake();

            path = new NavMeshPath();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsServer && FollowPath)
            {
                if (path.corners.Length > 1)
                {
                    var nearestCorner = path.corners[1];

                    if (Vector3.Distance(transform.position, path.corners[1]) > 0.1f && path.corners.Length > 2)
                    {
                        nearestCorner = path.corners[2];
                    }

                    var vector = nearestCorner - transform.position;
                    var input = new Vector2(vector.x, vector.z);

                    SetMovementVector(input);
                }
            }
        }

        protected abstract void AITick();

        private void OnDrawGizmosSelected()
        {
            if (path != null && path.corners.Length > 0)
            {
                var point = path.corners[0];

                for (int i = 1; i < path.corners.Length; i++)
                {
                    Gizmos.DrawLine(point, path.corners[i]);

                    point = path.corners[i];
                }
            }
            
        }

        private IEnumerator AITickProcess()
        {
            while (true)
            {
                if (Stunlock <= 0)
                {
                    AITick();

                    if (FollowPath && IsServer)
                    {
                        NavMesh.CalculatePath(transform.position, targetPosition, 1, path);
                    }
                }

                yield return new WaitForSecondsRealtime(0.2f);
            }
        }
    }
}
