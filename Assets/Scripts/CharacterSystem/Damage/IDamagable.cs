using System;
using CharacterSystem.Objects;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.DamageMath
{
    public interface IDamagable : 
        IGameObjectLink, 
        ITeammate
    {
        float health { get; set; }

        float stunlock { get; set; }
        
        VisualEffect OnHitEffect { get; }

        bool Hit(Damage damage);
        bool Heal(Damage damage);
        void Push(Vector3 direction);

        event Action<Damage> onDamageRecieved;

        void Kill();

        public bool isStunned => stunlock > 0;
    }
}