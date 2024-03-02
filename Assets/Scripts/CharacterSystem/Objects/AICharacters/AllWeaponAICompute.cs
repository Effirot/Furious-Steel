using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CharacterSystem.Attacks;
using UnityEngine;
using UnityEngine.AI;

namespace CharacterSystem.Objects.AI
{
    public class AllWeaponAICompute : AICompute
    {
        [SerializeField, Range(0, 50)]
        private float SearchRadius = 5;
            
        [SerializeField]
        private bool AllowUsingAttack = true;
        [SerializeField]
        private bool AllowUsingBlocks = false;
        [SerializeField]
        private bool AllowUsingUltimates = false;

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

        public override void StartAI()
        {
            // weaponOrigin = transform.Find();
        }
        public override void AITick()
        {
            if (target == null)
            {
                targetPosition = transform.position;

                target = ResearchTarget();
            }
            else
            {
                targetPosition = target.position;
            }
        }

        private DamageSource SelectDamageSource()
        {
            var sources = from source in damageSources 
                where source.IsActive
                select source;
             
            if (!sources.Any())
            {
                return null;
            }

            return sources.Last();   
        }
        private Cast[] GetDamageSourceCasts(DamageSource damageSource)
        {
            List<Cast> casts = new();

            GetCast (damageSource.queueElement);

            return casts.ToArray();
            
            void GetCast(AttackQueueElement element)
            {
                if (element is CharacterSystem.Attacks.Cast)
                {
                    casts.Add ((CharacterSystem.Attacks.Cast) element);

                    return;
                }

                if (element is CharacterSystem.Attacks.Queue)
                {   
                    var queue = (CharacterSystem.Attacks.Queue) element;

                    foreach (var queueElement in queue.queue)
                    {
                        GetCast (queueElement);
                    }
                }

                if (element is CharacterSystem.Attacks.Charger)
                {
                    var charger = (CharacterSystem.Attacks.Charger) element;

                    if (charger.chargeListener is AttackQueueElement)
                    {
                        GetCast ((AttackQueueElement) charger.chargeListener);
                    }
                }
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