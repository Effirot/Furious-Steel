using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CharacterSystem.Objects.AI
{   
    [RequireComponent(typeof(AICompute))]
    public class AINetworkCharacterMovement : NetworkCharacter
    {       
        public int PatchLayerIndex = 0;

        private NavMeshPath path;

        private AICompute AICompute; 


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
            AICompute = GetComponent<AICompute>();
        }
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsServer && AICompute.followPath)
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

                    movementVector = input;
                    
                    if (AICompute.lookDirection.magnitude <= 0)
                    {
                        lookVector = input;
                    }
                }

                if (AICompute.lookDirection.magnitude > 0)
                {
                    lookVector = AICompute.lookDirection;
                }

            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            if (path != null && path.corners.Length > 0)
            {
                var point = path.corners[0];

                for (int i = 1; i < path.corners.Length; i++)
                {
                    Gizmos.DrawLine(point, path.corners[i]);

                    point = path.corners[i];
                }
            }

            Gizmos.DrawWireCube(AICompute.targetPosition, Vector3.one);
            
        }

        private IEnumerator AITickProcess()
        {
            while (true)
            {
                if (stunlock <= 0)
                {
                    AICompute.AITick();

                    if (AICompute.followPath && IsServer)
                    {
                        NavMesh.CalculatePath(transform.position, AICompute.targetPosition, PatchLayerIndex, path);
                    }
                }

                yield return new WaitForSecondsRealtime(0.2f);
            }
        }
    }
}
