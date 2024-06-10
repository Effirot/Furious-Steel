using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using CharacterSystem.PowerUps;
using Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
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
        IPowerUpActivator,
        IObservableObject
    {
        public static bool AllowChampionMode = true;

        public delegate void OnPlayerCharacterStateChangedDelegate (PlayerNetworkCharacter character);

        public static event OnPlayerCharacterStateChangedDelegate OnPlayerCharacterDead = delegate { };
        public static event OnPlayerCharacterStateChangedDelegate OnPlayerCharacterSpawn = delegate { };

        public static event OnPlayerCharacterStateChangedDelegate OnOwnerPlayerCharacterDead = delegate { };
        public static event OnPlayerCharacterStateChangedDelegate OnOwnerPlayerCharacterSpawn = delegate { };
        
        
        public static PlayerNetworkCharacter Owner { get; private set; }

        public static List<PlayerNetworkCharacter> Players = new();

        [Space]
        [Header("Player")]
        [SerializeField]
        private InputActionReference moveInput;
        [SerializeField]
        private InputActionReference lookInput;
        [SerializeField]
        private InputActionReference jumpInput;
        [SerializeField]
        private InputActionReference killBindInput;

        [SerializeField]
        private CinemachineImpulseSource OnHitImpulseSource;

        [SerializeField]
        private GameObject suicideCorpsePrefab;

        [field : SerializeField]
        public Transform ObservingPoint { get; private set; }


        public ulong ServerClientID => network_serverClientId.Value;
        public int ClientDataIndex => RoomManager.Singleton.IndexOfPlayerData(data => data.ID == ServerClientID);
        public RoomManager.PublicClientData ClientData 
        { 
            get => RoomManager.Singleton.playersData[ClientDataIndex];
            set => RoomManager.Singleton.playersData[ClientDataIndex] = value;
        }

        public DamageBlocker Blocker { get; set; }
        public DamageDeliveryReport lastReport { 
            get => network_lastReport.Value; 
            set
            {
                if (IsServer)
                {
                    network_lastReport.Value = value;
                }
            } 
        }

        public int Combo 
        {
            get => network_combo.Value;
            private set {
                if (IsServer)
                {
                    if (value > network_combo.Value)
                    {
                        if (comboResetTimer != null && !this.IsUnityNull())
                        {
                            StopCoroutine(comboResetTimer);
                            comboResetTimer = null;
                        }

                        comboResetTimer = StartCoroutine(ComboResetTimer());
                    }

                    network_combo.Value = Mathf.Clamp(value, 0, 30);
                }
            }
        }

        public int PowerUpId { 
            get => network_powerUpId.Value; 
            set {
                if (IsServer)
                {
                    network_powerUpId.Value = value;
                }
            }
        }

        public bool JumpButtonState => network_jumpPressState.Value;

        private NetworkVariable<bool> network_jumpPressState = new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> network_powerUpId = new (-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<DamageDeliveryReport> network_lastReport = new (new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<ulong> network_serverClientId = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> network_combo = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public UnityEvent<PowerUp> onPowerUpChanged { get; } = new();



        public event Action<DamageDeliveryReport> onDamageDelivered;
        public event Action<int> onComboChanged;

        private Coroutine comboResetTimer = null;

        [HideInInspector]
        public Vector2 internalMovementVector = Vector2.zero;
        [HideInInspector]
        public Vector2 internalLookVector = Vector2.zero;

        public override bool Hit(Damage damage)
        {
            var isBlocked = Blocker != null && Blocker.Block(ref damage) || base.Hit(damage);

            if (isBlocked)
            {                
                Combo += 10;               
            }

            return isBlocked;
        }
        public virtual void DamageDelivered(DamageDeliveryReport report)
        {
            if (IsServer && report.isDelivered)
            {            
                if (report.damage.type is not Damage.Type.Effect)
                {
                    if (report.isBlocked)
                    {
                        Combo = 0;
                    }
                    else
                    {
                        Combo += 1;
                    }
                }

                var data = ClientData;
                data.statistics.DeliveredDamage += report.damage.value;

                if (report.target is PlayerNetworkCharacter && report.isLethal)
                {
                    data.statistics.Points += 1;
                    data.statistics.KillStreakTotal += 1;
                }

                ClientData = data;

                if (AllowChampionMode && data.statistics.Points >= 10 && TryGetComponent<CharacterEffectsHolder>(out var component))
                {
                    component.AddEffect(new ChampionModeEffect());
                }   
                
                onDamageDelivered?.Invoke(report);
            }

            
            if (IsOwner && report.damage.type is not Damage.Type.Effect)
            {
                OnHitImpulseSource?.GenerateImpulse(report.damage.value / 10f);
            }
        }

        protected override void OnHitReaction(Damage damage)
        {
            base.OnHitReaction(damage);

            if (IsOwner && damage.type is not Damage.Type.Effect)
            {
                OnHitImpulseSource?.GenerateImpulse(damage.value / 8f);
            }
        }

        public override void Kill()
        {
            base.Kill();

            if (IsServer)
            {
                var data = ClientData;

                data.statistics.Points = 0;
                data.statistics.AssistsStreak = 0;

                ClientData = data;
            }
        }

        public virtual void OnWeaponChanged(NetworkObject networkObject) { }
        public virtual void OnTrinketChanged(NetworkObject networkObject) { }

        public override void OnNetworkSpawn()
        {
            RoomManager.Singleton.playersData.OnListChanged += OnOwnerPlayerDataChanged_event;
            network_powerUpId.OnValueChanged += (Old, New) => onPowerUpChanged.Invoke(PowerUp.IdToPowerUpLink(New));

            network_combo.OnValueChanged += (Old, New) => {
                onComboChanged?.Invoke(New);

                CurrentSpeed += (New - Old) / 12f;
            };
            
            Players.Add(this);

            base.OnNetworkSpawn();

            if (IsOwner)
            {
                Owner = this;
                
                CharacterCameraObserver.Singleton.ObservingObject = this;

                if (moveInput != null) {
                    var action = moveInput.action;
                    action.Enable();

                    action.performed += OnMove;
                    action.canceled += OnMove;
                }

                if (lookInput != null) {
                    var action = lookInput.action;
                    action.Enable();

                    action.performed += OnLook;
                    action.canceled += OnLook;
                }

                if (jumpInput != null) {
                    var action = jumpInput.action;
                    action.Enable();

                    action.performed += OnJump;
                    action.canceled += OnJump;
                                        
                    isGroundedEvent += isGrounded => {
                        if (JumpButtonState && isGrounded)
                        {
                            Jump(movementVector);
                        }
                    };
                }

                if (killBindInput != null) {
                    var action = killBindInput.action;
                    action.Enable();

                    action.performed += KillBind;
                    action.canceled += KillBind;
                }

                OnOwnerPlayerCharacterSpawn.Invoke(this);
            }
            if (IsServer)
            {
                network_serverClientId.Value = OwnerClientId;
            }

            UpdateName();
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
                    var action = jumpInput.action;

                    action.performed -= OnJump;
                    action.canceled -= OnJump;
                }

                {
                    var action = killBindInput.action;

                    action.performed -= KillBind;
                    action.canceled -= KillBind;
                }
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsLocalPlayer)
            {
                if (permissions.HasFlag(CharacterPermission.AllowMove))
                {
                    movementVector = internalMovementVector;
                }

                if (permissions.HasFlag(CharacterPermission.AllowRotate))
                {
                    lookVector = internalLookVector;
                }
            }
        }

        protected override void Dead()
        {
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

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            var lookPoint = new Vector3(internalLookVector.x, 0, internalLookVector.y) + transform.position;

            Gizmos.DrawWireSphere(lookPoint, 0.2f);
        }

        protected override GameObject SpawnCorpse()
        {
            var corpseObject = base.SpawnCorpse();

            if (IsOwner && !corpseObject.IsUnityNull() && corpseObject.TryGetComponent<IObservableObject>(out var observableObject))
            {
                CharacterCameraObserver.Singleton.ObservingObject = observableObject;
            }

            return corpseObject;
        }

        protected virtual void OnOwnerPlayerDataChanged(NetworkListEvent<RoomManager.PublicClientData> changeEvent)
        {
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
                gameObject.name = name = ClientData.Name.Value;
            }
            catch { }
        }

        private IEnumerator ComboResetTimer()
        {
            yield return new WaitForSeconds(1f);

            var reduceTimeout = 0.6f;

            while (Combo > 0)
            {
                yield return new WaitForSeconds(reduceTimeout);

                reduceTimeout *= 0.92f;    
                Combo -= 1;
            }

            Combo = 0;

            comboResetTimer = null;
        }


        private void OnMove(CallbackContext input)
        {
            internalMovementVector = input.ReadValue<Vector2>();
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

                    internalLookVector = new Vector2(vector.x, vector.z);
                }
            }

            if (input.control.device is XInputController)
            {
                internalLookVector = value;
            }
        }
        private void OnJump(CallbackContext input)
        {
            if (network_jumpPressState.Value = input.ReadValueAsButton())
            {
                Jump(movementVector);
            }
        }

        private void KillBind(CallbackContext input)
        {
            KillBind_ServerRpc();
        }


        [ServerRpc]
        private void KillBind_ServerRpc()
        {
            if (!this.IsUnityNull())
            {
                if (IsServer && IsSpawned)
                {
                    OnSuicide_ClientRpc();
                }
            
                Damage.Deliver(this, new Damage(maxHealth, null, 100, Vector3.up, Damage.Type.Unblockable));
            }
        } 

        [ClientRpc]
        private void OnSuicide_ClientRpc()
        {
            if (suicideCorpsePrefab != null)
            {
                var corpse = Instantiate(suicideCorpsePrefab, transform.position, transform.rotation);
                
                corpse.SetActive(true); 
                
                Destroy(corpse, 5);
            }
        }    
    }
}