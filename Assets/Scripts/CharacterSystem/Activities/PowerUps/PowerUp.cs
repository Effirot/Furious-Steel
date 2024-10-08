using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.Objects;
using UnityEngine;


namespace CharacterSystem.PowerUps
{
    public abstract class PowerUp
    {
        public static PowerUp[] AllPowerUps = GetAllTypes();

        public static int GetTypeID<T>() where T : PowerUp
        {
            return Array.IndexOf(AllPowerUps, typeof(T));
        }

        public static PowerUp IdToPowerUpLink(int PowerUpId)
        {
            if (PowerUpId < 0 || PowerUpId >= PowerUp.AllPowerUps.Length)
            {
                return null;
            }
            else
            {
                return PowerUp.AllPowerUps[PowerUpId];
            }
        } 
        public static int PowerUpLinkToID(PowerUp powerUp)
        {
            if (powerUp == null)
            {
                return -1;
            }
            else
            {
                return Array.IndexOf(PowerUp.AllPowerUps, powerUp);
            }
        } 

        private static PowerUp[] GetAllTypes()
        {
            var types = typeof(PowerUp).Assembly.GetTypes();

            var array = from type in types
                where type.IsSubclassOf(typeof(PowerUp)) && !type.IsAbstract
                select Activator.CreateInstance(type) as PowerUp;

            return array.ToArray();
        }

        protected PowerUp() { }

        public virtual bool Undestroyable => false;

        public virtual bool IsOneshot => false;

        public virtual GameObject prefab => Resources.Load<GameObject>($"PowerUps/{this.GetType().Name}");

        public abstract void Activate(PowerUpHolder holder);
        
        public virtual bool CanPick(PowerUpHolder holder) => true;

        public virtual void OnPick(PowerUpHolder holder) { }
        public virtual void OnLost(PowerUpHolder holder) { }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
