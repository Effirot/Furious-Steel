using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CharacterSystem.Objects.AI
{   
    public abstract class AICompute
    {
        public class DistanceComparer : 
            IComparer<Component>
        {
            public int Compare(Component x, Component y)
            {
                return Mathf.RoundToInt(Vector3.Distance(y.transform.position, x.transform.position) * 100f);
            }
        }

        public Transform transform { get; internal set; }
        public GameObject gameObject { get; internal set; }

        public AINetworkCharacter Character { get; internal set; }

        public bool followPath { get; protected set; } = true;
        public Vector3? targetPosition { get; protected set; } = Vector3.zero;
        public Vector3 lookDirection { get; protected set; } = Vector3.zero;

        public abstract Task AITick();
        public abstract void StartAI();

        public virtual void OnValidate() { }
    }
}