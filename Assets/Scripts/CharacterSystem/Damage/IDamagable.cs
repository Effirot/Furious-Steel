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
        public float maxHealth { get; }
        public float health { get; set; }

        public float stunlock { get; set; }

        public Damage lastRecievedDamage { get; set; }

        public bool isStunned => stunlock > 0;


        public event Action<Damage> onDamageRecieved;


        public bool Hit(Damage damage);
        public bool Heal(Damage damage);
        public void Push(Vector3 direction);

        public void Kill();
    }
}