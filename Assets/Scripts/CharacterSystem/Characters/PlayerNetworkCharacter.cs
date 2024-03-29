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
        [SerializeField]
        private InputActionReference dashInput;

        [SerializeField]
        private CinemachineImpulseSource OnHitImpulseSource;

        public ulong ServerClientID => network_serverClientId.Value;
        public int ClientDataIndex => RoomManager.Singleton.IndexOfPlayerData(data => data.ID == ServerClientID);
        public RoomManager.PublicClientData ClientData 
        { 
            get => RoomManager.Singleton.playersData[ClientDataIndex];
            set => RoomManager.Singleton.playersData[ClientDataIndex] = value;
        }

        public DamageBlocker Blocker { get; set; }

        private NetworkVariable<ulong> network_serverClientId = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public event Action<DamageDeliveryReport> OnDamageDelivered;


        public override bool Hit(Damage damage)
        {
            var isBlocked = Blocker != null && Blocker.Block(ref damage);

            if (damage.value != 0)
            {
                base.Hit(damage); 
            }

            if (isBlocked)
            {
                RechargeDodge();
            }

            if (IsOwner)
            {
                OnHitImpulseSource?.GenerateImpulse();
            }

            return isBlocked;
        }

        public virtual void DamageDelivered(DamageDeliveryReport report)
        {
            if (IsServer)
            {
                var data = ClientData;
                data.statistics.DeliveredDamage += report.damage.value;

                if (report.target is PlayerNetworkCharacter && report.isLethal)
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

                {
                    var action = dashInput.action;
                    action.Enable();

                    action.performed += OnDash;
                    action.canceled += OnDash;
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

                {
                    var action = dashInput.action;

                    action.performed -= OnDash;
                    action.canceled -= OnDash;
                }
            }
        }

        protected override void Dead()
        {
            if (IsClient && CorpsePrefab != null)
            {
                var corpseObject = Instantiate(CorpsePrefab, transform.position, transform.rotation);
                corpseObject.transform.localScale = transform.localScale;

                Destroy(corpseObject, 10);

                if (IsOwner)
                {
                    CharacterUIObserver.Singleton.observingCharacter = corpseObject.transform;
                }    
            }

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
                changeEvent.Type != NetworkListEvent<RoomManager.PublicClientData>.EventType.RemoveAt ||
                changeEvent.Type != NetworkListEvent<RoomManager.PublicClientData>.EventType.Clear) &&
                IsSpawned)
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

        private void OnMove(CallbackContext input)
        {
            movementVector = input.ReadValue<Vector2>();
        }

        private void OnLook(CallbackContext input)
        {
            var value = input.ReadValue<Vector2>();

            if (input.control.device is Mouse)
            {
                var ray = Camera.main.ScreenPointToRay(value);

                Plane plane = new Plane(Vector3.up, transform.position);

                if (plane.Raycast(ray, out float enter))
                {
                    var vector = ray.GetPoint(enter) - transform.position;

                    lookVector = new Vector2(vector.x, vector.z);
                }    
            }

            if (input.control.device is XInputController)
            {
                lookVector = value;
            }
        }
        private void OnDash(CallbackContext input)
        {
            if (movementVector.magnitude > 0 && input.ReadValueAsButton())
            {
                Dash(movementVector);
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
                var clientdata = ClientData;

                foreach(var paintable in GetComponentsInChildren<IPaintable>())
                {
                    
                    paintable.SetColor(ClientData.spawnArguments.GetColor());
                    paintable.SetSecondColor(ClientData.spawnArguments.GetSecondColor());
                }
            }
        }
    }
}