using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CharacterSystem.Objects.AI
{   
    public class AINetworkCharacterMovement : NetworkCharacter
    {       
        public int PatchLayerIndex = 0;

        private NavMeshPath path;

        [SerializeField, SerializeReference, SubclassSelector]
        private AICompute AICompute = new AllWeaponAICompute(); 


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                AICompute?.StartAI();

                StartCoroutine(AITickProcess());
            }
            else 
            {
                AICompute = null;
            }
        }

        protected override void Awake() 
        {
            base.Awake();

            SetDataToAICompute();

            path = new NavMeshPath();
        }
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            FollowPath();
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

            if (Application.isPlaying && AICompute != null)
            {
                Gizmos.DrawWireCube(AICompute.targetPosition, Vector3.one);
            }
        }
        protected override void OnValidate()
        {
            base.OnValidate();

            SetDataToAICompute();

            AICompute?.OnValidate();
        }

        private void SetDataToAICompute()
        {
            if (AICompute != null)
            {
                AICompute.transform = transform;
                AICompute.gameObject = gameObject;
                AICompute.Character = this;
            }
        }
        private void FollowPath()
        {
            if (IsServer && AICompute != null && AICompute.followPath)   
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
