using System;
using CharacterSystem.Attacks;
using CharacterSystem.Objects;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.DamageMath
{
    public interface IDamagable : 
        IGameObjectLink, 
        ITeammate
    {
        float maxHealth { get; }
        float health { get; set; }

        float stunlock { get; set; }

        Damage lastRecievedDamage { get; set; }

        bool Hit(Damage damage);
        bool Heal(Damage damage);
        void Push(Vector3 direction);

        event Action<Damage> onDamageRecieved;

        void Kill();

        public bool isStunned => stunlock > 0;
    }
}