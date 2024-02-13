using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.VFX;
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
        private float DodgeRechargeTime = 2f;

        [SerializeField]
        private VisualEffect DodgeEffect;

        [SerializeField]
        private AudioSource DodgeSound;


        private Coroutine dodgeRechargeRoutine = null;
        private float lastTapTime = 0;

        public void RefreshColor()
        {
            RefreshColor_ClientRpc();
        }

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

            RefreshColor_Internal();
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
            var value = input.ReadValue<Vector2>();
            SetMovementVector(value);

            var multiTapDelayTime = InputSystem.settings.multiTapDelayTime;

            if (input.action.WasPressedThisFrame())
            {
                if ((Time.time - lastTapTime) < multiTapDelayTime)
                {
                    Dash_ServerRpc(value);
                }
    
                lastTapTime = Time.time;
            }
        }

        private void Dash_Internal(Vector2 direction)
        {           
            if (dodgeRechargeRoutine == null)
            {
                var V3direction = new Vector3(direction.x, 0, direction.y);

                Push(V3direction * DodgePushForce);   
                dodgeRechargeRoutine = StartCoroutine(DodgeRechargeRoutine());

                DodgeEffect.SetVector3("Direction", V3direction);
                DodgeEffect.Play();

                DodgeSound.Play();
            }
        }

        [ClientRpc]
        private void Dash_ClientRpc(Vector2 direction)
        {      
            if (!IsServer)
            {
                Dash_Internal(direction);
            }    
        }
        [ServerRpc]
        private void Dash_ServerRpc(Vector2 direction)
        {
            direction.Normalize();

            if (IsServer && !isStunned)
            {
                SetAngle(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y);
            }

            Dash_ClientRpc(direction);
            Dash_Internal(direction);
        }

        [ClientRpc]
        private void RefreshColor_ClientRpc()
        {
            RefreshColor_Internal();
        }
        private void RefreshColor_Internal()
        {
            var playerData = RoomManager.Singleton.FindClientData(OwnerClientId);
            
            foreach(var paintable in GetComponentsInChildren<IPaintable>())
            {
                paintable.SetColor(playerData.spawnArguments.GetColor());
                paintable.SetSecondColor(playerData.spawnArguments.GetSecondColor());
            }
        }
    }
}