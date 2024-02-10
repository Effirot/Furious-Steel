using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
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

        public static List<PlayerNetworkCharacter> Players = new();

        [Header("Player")]
        [SerializeField]
        private InputActionReference moveInput;

        [Header("Dodge")]
        [SerializeField]
        [Range(0, 400)]
        private float DodgePushForce = 130;

        [SerializeField]
        [Range(0, 400)]
        private float DodgeRechargeTime = 0.7f;

        
        private Coroutine dodgeRechargeRoutine = null;

        public override void OnNetworkSpawn()
        {
            Players.Add(this);

            base.OnNetworkSpawn();

            if (IsOwner)
            {
                Owner = this;

                var action = moveInput.action;
                action.Enable();

                action.performed += OnMove;
                action.canceled += OnMove;
            
                OnOwnerPlayerCharacterSpawn.Invoke(this);
            }


        }
        public override void OnNetworkDespawn()
        {
            Players.Remove(this);

            base.OnNetworkDespawn();

            if (IsOwner)
            {
                Owner = null;

                var action = moveInput.action;
                action.performed -= OnMove;
                action.canceled -= OnMove;
            }
        }

        protected override void Dodge(Vector2 direction)
        {
            if (IsServer && dodgeRechargeRoutine == null)
            {
                var V3direction = new Vector3(direction.x, 0, direction.y);

                Push(V3direction * DodgePushForce);   

                SetAngle(Quaternion.LookRotation(V3direction).eulerAngles.y);

                dodgeRechargeRoutine = StartCoroutine(DodgeRechargeRoutine());
            }
        }
        protected override void OnMoveVectorChanged(Vector2 oldMovementVector, Vector2 newMovementVector)
        {

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
        }

        private IEnumerator DodgeRechargeRoutine()
        {
            yield return new WaitForSeconds(DodgeRechargeTime);

            dodgeRechargeRoutine = null;
        }

        private void OnMove(CallbackContext input)
        {
            SetMovementVector(input.ReadValue<Vector2>());

            if (input.interaction is MultiTapInteraction)
            {
                Dodge(input.ReadValue<Vector2>());
            }
        }
    }
}