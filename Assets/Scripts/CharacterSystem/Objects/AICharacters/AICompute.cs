using Unity.Netcode;
using UnityEngine;

namespace CharacterSystem.Objects.AI
{   
    public abstract class AICompute
    {
        public Transform transform { get; internal set; }
        public GameObject gameObject { get; internal set; }

        public AINetworkCharacterMovement Character { get; internal set; }

        public bool followPath { get; protected set; } = true;
        public Vector3 targetPosition { get; protected set; } = Vector3.zero;
        public Vector3 lookDirection { get; protected set; } = Vector3.zero;

        public abstract void AITick();
        public abstract void StartAI();

        public virtual void OnValidate() { }
    }
}