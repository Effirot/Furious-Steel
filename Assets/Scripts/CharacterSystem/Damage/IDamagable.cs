using System;
using CharacterSystem.Objects;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.DamageMath
{
    public interface IDamagable : IMonoBehaviourLink 
    {
        float health { get; set; }

        float stunlock { get; set; }

        bool IsAlive { get; }
        
        VisualEffect OnHitEffect { get; }

        bool Hit(Damage damage);
        bool Heal(Damage damage);
        void Push(Vector3 direction);

        void Kill();

        public bool isStunned => stunlock > 0;
    }
}