using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using CharacterSystem.PowerUps;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.XInput;
using UnityEngine.VFX;
using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Objects
{
    public class PlayerNetworkCharacter : NetworkCharacter,
        IDamageSource,
        IDamageBlocker,
        IPowerUpActivator
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
        [SerializeField]
        private InputActionReference lookInput;

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
        public int ClientDataIndex => RoomManager.Singleton.IndexOfPlayerData(data => data.ID == ServerClientID);
        public RoomManager.PublicClientData ClientData 
        { 
            get => RoomManager.Singleton.FindClientData(ServerClientID);
            set 
            {            
                RoomManager.Singleton.playersData[ClientDataIndex] = value;
            }
        }

        public DamageBlocker Blocker { get; set; }

        private NetworkVariable<ulong> network_serverClientId = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public event Action<DamageDeliveryReport> OnDamageDelivered;

        private Coroutine dodgeRoutine = null;
        private float lastTapTime = 0;

        public override bool Hit(Damage damage)
        {
            if (health <= 0 && damage.sender is NetworkCharacter)
            {
                CharacterUIObserver.Singleton.observingCharacter = (NetworkCharacter) damage.sender;
            }

            return (Blocker != null && Blocker.Block(ref damage)) || base.Hit(damage);
        }

        public virtual void DamageDelivered(DamageDeliveryReport report)
        {
            if (IsServer)
            {
                var data = ClientData;
                data.statistics.DeliveredDamage += report.damage.value;

                if (report.isLethal)
                {
                    data.statistics.KillStreak += 1;
                    data.statistics.KillStreakTotal += 1;
                }

                ClientData = data;
            }

            OnDamageDelivered?.Invoke(report);
        }

        public void RefreshColor()
        {
            RefreshColor_ClientRpc();
        }
        
        public override void Kill()
        {
            base.Kill();

            if (IsServer)
            {
                var data = ClientData;

                data.statistics.KillStreak = 0;
                data.statistics.AssistsStreak = 0;

                ClientData = data;
            }
        }

        public override void OnNetworkSpawn()
        {
            RoomManager.Singleton.playersData.OnListChanged += OnOwnerPlayerDataChanged_event;
            Players.Add(this);

            base.OnNetworkSpawn();

            if (IsOwner)
            {
                Owner = this;
                
                {
                    var action = moveInput.action;
                    action.Enable();

                    action.performed += OnMove;
                    action.canceled += OnMove;
                }

                {
                    var action = lookInput.action;
                    action.Enable();

                    action.performed += OnLook;
                    action.canceled += OnLook;
                }
            
                OnOwnerPlayerCharacterSpawn.Invoke(this);
            }

            if (IsServer)
            {
                network_serverClientId.Value = OwnerClientId;
            }

            UpdateName();
            RefreshColor_Internal();
        }
        public override void OnNetworkDespawn()
        {
            RoomManager.Singleton.playersData.OnListChanged -= OnOwnerPlayerDataChanged_event;

            Players.Remove(this);

            base.OnNetworkDespawn();

            if (IsOwner)
            {
                Owner = null;
                
                {
                    var action = moveInput.action;
                    
                    action.performed -= OnMove;
                    action.canceled -= OnMove;
                }
                
                {
                    var action = lookInput.action;

                    action.performed -= OnLook;
                    action.canceled -= OnLook;
                }
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


        protected virtual void OnOwnerPlayerDataChanged(NetworkListEvent<RoomManager.PublicClientData> changeEvent)
        {
            if (changeEvent.Value.spawnArguments.ColorScheme != changeEvent.PreviousValue.spawnArguments.ColorScheme)
            {
                RefreshColor_Internal();
            }

            UpdateName();
        }

        private void OnOwnerPlayerDataChanged_event(NetworkListEvent<RoomManager.PublicClientData> changeEvent)
        {
            if (changeEvent.Value.ID == ServerClientID && 
                (changeEvent.Type != NetworkListEvent<RoomManager.PublicClientData>.EventType.Remove || 
                changeEvent.Type != NetworkListEvent<RoomManager.PublicClientData>.EventType.RemoveAt))
            {
                OnOwnerPlayerDataChanged(changeEvent);
            }
        }

        private void UpdateName()
        {
            try 
            {
                gameObject.name = name = $"Player({ClientData.Name.Value})";
            }
            catch { }
        }

        private IEnumerator DodgeRoutine(Vector3 Direction)
        {
            Direction.Normalize();
            // Push(V3direction * DodgePushForce);  

            permissions = CharacterPermission.Untouchable;
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

            dodgeRoutine = null;
        }

        private void OnMove(CallbackContext input)
        {
            var value = input.ReadValue<Vector2>();
            movementVector = value;

            var multiTapDelayTime = InputSystem.settings.multiTapDelayTime;

            if (input.action.WasPressedThisFrame())
            {
                if ((Time.time - lastTapTime) < multiTapDelayTime)
                {
                    Dash(value);
                }
    
                lastTapTime = Time.time;
            }
        }
        private void OnLook(CallbackContext input)
        {
            var value = input.ReadValue<Vector2>();

            if (input.control.device is Mouse)
            {
                var ray = Camera.main.ScreenPointToRay(value);

                if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
                {
                    var vector = hit.point - transform.position;
                    
                    lookVector = new Vector2(vector.x, vector.z);
                }
            }
            
            if (input.control.device is XInputController)
            {
                lookVector = value;
            }
        }
        
        public void Dash(Vector2 direction)
        {
            if (IsOwner)
            {
                Dash_ServerRpc(direction);
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

            if (IsServer && !isStunned && dodgeRoutine == null && permissions.HasFlag(CharacterPermission.AllowDash))
            {
                SetAngle(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y);

                Dash_ClientRpc(direction);
                Dash_Internal(direction);
            }
        }
        private void Dash_Internal(Vector2 direction)
        {           
            if (dodgeRoutine == null && permissions.HasFlag(CharacterPermission.AllowDash))
            {
                var V3direction = new Vector3(direction.x, 0, direction.y);

                dodgeRoutine = StartCoroutine(DodgeRoutine(V3direction));
                
                if (IsClient) {
                    DodgeEffect.SetVector3("Direction", V3direction);
                    DodgeEffect.Play();

                    DodgeSound.Play();
                }
            }
        }

        [ClientRpc]
        private void RefreshColor_ClientRpc()
        {
            RefreshColor_Internal();
        }
        private void RefreshColor_Internal()
        {            
            if (IsClient)
            {
                foreach(var paintable in GetComponentsInChildren<IPaintable>())
                {
                    paintable.SetColor(ClientData.spawnArguments.GetColor());
                    paintable.SetSecondColor(ClientData.spawnArguments.GetSecondColor());
                }
            }
        }
    }
}