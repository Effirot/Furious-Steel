using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using UnityEngine;
using UnityEngine.AI;

namespace CharacterSystem.Objects.AI
{
    public class AllWeaponAICompute : AICompute
    {
        [SerializeField, Range(0, 50)]
        private float SearchRadius = 5;

        public Transform WeaponOrigin
        {
            get => weaponOrigin;
            set
            {
                damageSources.Clear();

                if (value != null)
                {
                    damageSources.AddRange(value.GetComponentsInChildren<DamageSource>());
                }

                weaponOrigin = value;
            }
        }

        private List<DamageSource> damageSources = new();
        
        private Transform weaponOrigin = null;
        private Transform target = null;

        public override void AITick()
        {
            if (target == null)
            {
                target = ResearchTarget();
                
                targetPosition = transform.position;
            }
            else
            {
                targetPosition = target.position;
            }
        }

        private Transform ResearchTarget()
        {
            var characters = Physics.OverlapSphere(transform.position, SearchRadius, LayerMask.GetMask("Character"));
            
            var character = System.Array.Find(characters, item => item.gameObject.tag == "Player");
            
            if (character != null)
            {
                return character.transform;
            }
            
            return null;
        }
    }
}