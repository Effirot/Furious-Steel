using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using Cysharp.Threading.Tasks;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CharacterSystem.Objects.AI
{   
    public class AINetworkCharacter : NetworkCharacter, 
        IAttackSource,
        IDamageBlockerAcivity
    {       
        private static Team botTeam = new(); 
        
        [Header("AI")]
        [SerializeField]
        public int PatchLayerIndex = 0;

        [SerializeField, SerializeReference, SubclassSelector]
        private AICompute AICompute = new AllWeaponAICompute();


        private NavMeshPath path;

        public DamageBlockerAcivity Blocker { get; set; }
        public DamageDeliveryReport lastReport { get; set; }

        public int Combo => 0;

        float IAttackSource.DamageMultipliyer { get => DamageMultipliyer; set => DamageMultipliyer = value; }

        [SyncVar]
        private float DamageMultipliyer = 1f;

        public event Action<DamageDeliveryReport> onDamageDelivered;
        public event Action<int> onComboChanged = delegate { };

        public void DamageDelivered(DamageDeliveryReport report)
        {
            onDamageDelivered?.Invoke(report);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            team = botTeam;

            if (isServer)
            {
                AICompute?.StartAI();

                AITickProcess();
            }
            else 
            {
                AICompute = null;
            }
        }
        public override void OnStopServer()
        {
            base.OnStopServer();
            
            ClearAICompute();
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

            if (Application.isPlaying && AICompute != null && AICompute.targetPosition.HasValue)
            {
                Gizmos.DrawWireCube(AICompute.targetPosition.Value, Vector3.one);
            }
        }
        protected override void OnDrawGizmos()
        {            
            base.OnDrawGizmos();
            
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
        protected override void OnValidate()
        {
            base.OnValidate();

            SetDataToAICompute();

            AICompute?.OnValidate();

            RemoveAllActivityesInput();
        }

        public void RemoveAllActivityesInput()
        {
            foreach (var activity in GetComponentsInChildren<SyncedActivitySource>())
            {
                activity.inputAction = null;
            }
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
            if (isServer && AICompute != null)   
            {
                Vector2 input = Vector2.zero; 

                if (AICompute.followPath)
                {
                    if (path.corners.Length > 1)
                    {
                        var nearestCorner = path.corners[1];

                        if (Vector3.Distance(transform.position, path.corners[1]) > 0.1f && path.corners.Length > 2)
                        {
                            nearestCorner = path.corners[2];
                        }

                        var vector = nearestCorner - transform.position;
                        input = new Vector2(vector.x, vector.z);
                    }
                    else
                    {
                        if (AICompute.targetPosition != null)
                        {
                            var vector = AICompute.targetPosition - transform.position;
                            input = new Vector2(vector.Value.x, vector.Value.z);
                        }
                    }
                }

                lookVector = AICompute.lookDirection.magnitude > 0 ? input : AICompute.lookDirection;

                movementVector = input;
            }
        }

        private void ClearAICompute()
        {
            if (AICompute != null)
            {
                GC.SuppressFinalize(AICompute);
            }
            
            AICompute = null;
        }

        private async void AITickProcess()
        {
            while (NetworkServer.spawned.ContainsKey(netId))
            {
                if (stunlock <= 0)
                {
                    await AICompute.AITick();

                    if (AICompute != null &&
                        AICompute.followPath && 
                        AICompute.targetPosition.HasValue && 
                        isServer)
                    {
                        NavMesh.CalculatePath(transform.position, AICompute.targetPosition.Value, PatchLayerIndex, path);
                    }
                    else
                    {
                        path.ClearCorners();
                    }
                }

                await UniTask.Delay(100);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof (AINetworkCharacter), true)]
        protected class AINetworkCharacter_Editor : NetworkCharacter_Editor
        {
            private SerializedProperty patchLayerIndex;
            private SerializedProperty AICompute;

            public override void OnInspectorGUI()
            {
                patchLayerIndex ??= serializedObject.FindProperty("PatchLayerIndex");
                AICompute       ??= serializedObject.FindProperty("AICompute");

                EditorGUILayout.PropertyField(patchLayerIndex);
                EditorGUILayout.PropertyField(AICompute);

                base.OnInspectorGUI();
            }
        }
#endif
    }
}
