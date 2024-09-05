
using UnityEngine;

namespace CharacterSystem.Attacks
{
    public class AttackPattern : MonoBehaviour
    {
        [SerializeField, SerializeReference, SubclassSelector]
        public AttackQueueElement[] attackQueue;

        public void GetGizmos(Transform transform)
        {
            foreach (var item in attackQueue)
            {
                if (item != null)
                {
                    item.OnDrawGizmos(transform);
                } 
            }
        }
    } 

}