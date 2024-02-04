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
        public static event OnCharacterStateChangedDelegate OnPlayerCharacterDead = delegate { };
        public static event OnCharacterStateChangedDelegate OnPlayerCharacterSpawn = delegate { };

        [SerializeField]
        private InputActionReference moveInput;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                var action = moveInput.action;
                action.Enable();

                action.performed += OnMove;
                action.canceled += OnMove;

                ObservCharacter();
            }
        }

        protected override void Dead()
        {
            base.Dead();

            OnPlayerCharacterDead.Invoke(this);
        }

        protected override void Spawn()
        {
            base.Spawn();

            OnPlayerCharacterSpawn.Invoke(this);
        }

        private void ObservCharacter()
        {
            CharacterUI.Singleton.observingCharacter = this;
        }

        private void OnMove(CallbackContext input)
        {
            SetMovementVector(input.ReadValue<Vector2>());
        }
    }
}