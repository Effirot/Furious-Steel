using System.Collections;
using System.Collections.Generic;
using Cinemachine;
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
        [Range(0, 10)]
        private float DodgePushForce = 2;

        [SerializeField]
        [Range(0, 10)]
        private float DodgePushTime = 1;

        [SerializeField]
        [Range(0, 400)]
        private float DodgeRechargeTime = 2f;

        [SerializeField]
        private VisualEffect DodgeEffect;

        [SerializeField]
        private AudioSource DodgeSound;

        public ulong ServerClientID => network_serverClientId.Value;
        public RoomManager.PublicClientInfo ClientData => RoomManager.Singleton.FindClientData(ServerClientID);

        private NetworkVariable<ulong> network_serverClientId = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

            if (IsServer)
            {
                network_serverClientId.Value = OwnerClientId;
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

        private IEnumerator DodgeRoutine(Vector3 Direction)
        {
            Direction.Normalize();
            // Push(V3direction * DodgePushForce);  

            permissions = CharacterPermission.None;
            animator.SetBool("Dodge", true);

            var timer = 0f;
            while (timer < DodgePushTime)
            {
                timer += Time.fixedDeltaTime;
                characterController.Move(Direction * DodgePushForce);

                yield return new WaitForFixedUpdate();
            } 

            permissions = CharacterPermission.All;
            animator.SetBool("Dodge", false);

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

            if (IsServer && !isStunned && dodgeRechargeRoutine == null)
            {
                SetAngle(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y);

                Dash_ClientRpc(direction);
                Dash_Internal(direction);
            }
        }
        private void Dash_Internal(Vector2 direction)
        {           
            if (dodgeRechargeRoutine == null && permissions.HasFlag(CharacterPermission.AllowDash))
            {
                var V3direction = new Vector3(direction.x, 0, direction.y);

                dodgeRechargeRoutine = StartCoroutine(DodgeRoutine(V3direction));

                DodgeEffect.SetVector3("Direction", V3direction);
                DodgeEffect.Play();

                DodgeSound.Play();
            }
        }

        [ClientRpc]
        private void RefreshColor_ClientRpc()
        {
            RefreshColor_Internal();
        }
        private void RefreshColor_Internal()
        {            
            foreach(var paintable in GetComponentsInChildren<IPaintable>())
            {
                paintable.SetColor(ClientData.spawnArguments.GetColor());
                paintable.SetSecondColor(ClientData.spawnArguments.GetSecondColor());
            }
        }
    }
}