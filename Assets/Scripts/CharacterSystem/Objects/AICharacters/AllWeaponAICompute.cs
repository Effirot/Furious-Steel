using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CharacterSystem.Attacks;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace CharacterSystem.Objects.AI
{
    public class AllWeaponAICompute : AICompute
    {
        [SerializeField, Range(0, 50)]
        private float SearchRadius = 5;

        [SerializeField, Range(0, 3)]
        private float AfterAttackDelay = 0.3f;
        [SerializeField, Range(0, 3)]
        private float BeforeAttackDelay = 0.3f;
            
        [SerializeField]
        private bool AllowUsingAttack = true;
        [SerializeField]
        private bool AllowUsingBlocks = false;
        [SerializeField]
        private bool AllowUsingUltimates = false;

        public List<DamageSource> damageSources = new();
        
        private Transform target = null;
        private DamageSource selectedDamageSource; 

        public override void StartAI()
        {

        }
        public override async Task AITick()
        {
            target = ResearchTarget();

            if (target == null)
            {
                targetPosition = transform.position;
                lookDirection = Vector3.zero;
            }
            else
            {
                targetPosition = target.position;
                lookDirection = LookToTarget();

                if (selectedDamageSource?.IsActive ?? true)
                {
                    selectedDamageSource = SelectDamageSource();
                }
                else
                {
                    var cast = GetDamageSourceCasts(selectedDamageSource).First();
                    var casterValue = GetAttackVector(cast);
                    
                    var caster = casterValue.Item1;
                    var casterVector = casterValue.Item2;

                    var isAttackAvailable = 
                        !selectedDamageSource.IsAttacking && 
                        Array.Exists(caster.CastCollider(selectedDamageSource.transform), Collider => Collider.transform == target);

                    if (isAttackAvailable)
                    {
                        await Attack();
                    }
                }
            }
        }

        private async Task Attack()
        {
            followPath = false;
        
            await UniTask.WaitForSeconds(BeforeAttackDelay);
            selectedDamageSource.StartAttack();
            await UniTask.WaitUntil(() => selectedDamageSource.IsAttacking);
            await UniTask.WaitForSeconds(AfterAttackDelay);

            selectedDamageSource = null;
            followPath = true;
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
        private (Caster, Vector3) GetAttackVector(Cast cast)
        {
            foreach (var collider in cast.casters)
            {
                if (collider == null)
                    continue;

                if (collider is BoxCaster)
                {
                    var caster = (BoxCaster)collider;
                    return (caster, caster.position);
                }

                if (collider is SphereCaster)
                {
                    var caster = (SphereCaster)collider;
                    return (caster, caster.position);
                }

                if (collider is RaycastCaster)
                {
                    var caster = (RaycastCaster)collider;

                    return (caster, caster.direction + caster.origin);
                }

                if (collider is TargetRaycastCaster)
                {
                    var caster = (TargetRaycastCaster)collider;

                    if (caster.target == null)
                        continue;

                    return (caster, caster.target.position + caster.origin);
                }
            }
            
            return (null, Vector3.zero);
        }
        
        private Vector3 LookToTarget()
        {
            return Quaternion.LookRotation(transform.position - target.position).eulerAngles;
        }


        private Transform ResearchTarget()
        {
            Collider character = null;
            
            Collider[] characters = Physics.OverlapSphere(transform.position, SearchRadius, LayerMask.GetMask("Character"));
            
            Array.Sort(characters, new DistanceComparer());

            character = Array.Find(characters, item => item.gameObject.TryGetComponent<ITeammate>(out var teammate) && !ITeammate.IsAlly(Character, teammate));
            
            if (character != null)
            {
                return character.transform;
            }
            
            return null;
        }
    }
}