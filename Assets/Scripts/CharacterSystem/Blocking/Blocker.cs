using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CharacterSystem.Blocking
{
    public class Blocker : NetworkBehaviour
    {
        [SerializeField]
        private InputActionReference inputAction;

        [SerializeField, Range(0, 20)]
        private float Stunlock = 5;

        public virtual void Block(Damage damage)
        {
            if (damage.Sender != null)
            {
                damage.Sender.Stunlock = Stunlock;
            }
        } 
    }
}
