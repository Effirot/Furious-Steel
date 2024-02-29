using Unity.Netcode;
using UnityEngine;

namespace CharacterSystem.Objects.AI
{   
    [DisallowMultipleComponent]
    public abstract class AICompute : NetworkBehaviour
    {
        public bool followPath { get; protected set; } = true;

        public Vector3 targetPosition { get; protected set; } = Vector3.zero;

        public Vector3 lookDirection { get; protected set; } = Vector3.zero;

        public abstract void AITick();
    }
}