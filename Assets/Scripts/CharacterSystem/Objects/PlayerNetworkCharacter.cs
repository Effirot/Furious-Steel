using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Objects
{
    public class PlayerNetworkCharacter : NetworkCharacter
    {
        public delegate void OnPlayerCharacterStateChangedDelegate (PlayerNetworkCharacter character);

        public static event OnPlayerCharacterStateChangedDelegate OnPlayerCharacterDead = delegate { };
        public static event OnPlayerCharacterStateChangedDelegate OnPlayerCharacterSpawn = delegate { };

        public static event OnPlayerCharacterStateChangedDelegate OnOwnerPlayerCharacterDead = delegate { };
        public static event OnPlayerCharacterStateChangedDelegate OnOwnerPlayerCharacterSpawn = delegate { };

        public static PlayerNetworkCharacter Owner { get; private set; }

        [SerializeField]
        private InputActionReference moveInput;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                Owner = this;

                var action = moveInput.action;
                action.Enable();

                action.performed += OnMove;
                action.canceled += OnMove;
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                Owner = null;

                var action = moveInput.action;
                action.performed -= OnMove;
                action.canceled -= OnMove;
            }
        }

        protected override void Dead()
        {
            base.Dead();

            OnPlayerCharacterDead.Invoke(this);

            if (IsOwner)
            {
                OnOwnerPlayerCharacterDead.Invoke(this);
            }
        }
        protected override void Spawn()
        {
            base.Spawn();

            OnPlayerCharacterSpawn.Invoke(this);

            if (IsOwner)
            {
                OnOwnerPlayerCharacterSpawn.Invoke(this);
            }
        }

        private void OnMove(CallbackContext input)
        {
            SetMovementVector(input.ReadValue<Vector2>());
        }
    }
}