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
        public static bool AllowChampionMode = true;

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
        private InputActionReference killBindInput;

        [SerializeField]
        private CinemachineImpulseSource OnHitImpulseSource;

        [SerializeField]
        private GameObject suicideCorpsePrefab;

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
                    if (network_combo.Value != value && value > 0)
                    {
                        if (comboResetTimer != null)
                        {
                            StopCoroutine(comboResetTimer);
                            comboResetTimer = null;
                        }

                        comboResetTimer = StartCoroutine(ComboResetTimer());
                    }

                    network_combo.Value = value;
                }
            }
        }

        private NetworkVariable<DamageDeliveryReport> network_lastReport = new (new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<ulong> network_serverClientId = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> network_combo = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public event Action<DamageDeliveryReport> OnDamageDelivered;
        public event Action<int> OnComboChanged;

        private Coroutine comboResetTimer = null;

        public override bool Hit(Damage damage)
        {
            var isBlocked = Blocker != null && Blocker.Block(ref damage) || base.Hit(damage);            

            if (isBlocked)
            {
                RechargeDodge();
                
                Combo += 5;
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
                Combo += 1;

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

                data.statistics.Points = 0;
                data.statistics.AssistsStreak = 0;

                ClientData = data;
            }
        }
        public void Kill(bool IsSuicide)
        {
            this.Kill();

            if (IsSuicide && IsServer && IsSpawned)
            {
                OnSuicide_ClientRpc();
            }
        }

        public override void OnNetworkSpawn()
        {
            RoomManager.Singleton.playersData.OnListChanged += OnOwnerPlayerDataChanged_event;

            network_combo.OnValueChanged += (Old, New) => {
                OnComboChanged?.Invoke(New);
            };
            
            Players.Add(this);

            base.OnNetworkSpawn();

            if (IsOwner)
            {
                Owner = this;
                
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

                if (dashInput != null) {
                    var action = dashInput.action;
                    action.Enable();

                    action.performed += OnDash;
                    action.canceled += OnDash;
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

                {
                    var action = killBindInput.action;

                    action.performed -= KillBind;
                    action.canceled -= KillBind;
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

        private IEnumerator ComboResetTimer()
        {
            yield return new WaitForSeconds(0.5f);

            var recudeTimeout = 0.1f;
            while (network_combo.Value > 0)
            {
                yield return new WaitForSeconds(recudeTimeout);

                recudeTimeout -= 0.005f;
                
                network_combo.Value -= 1;
            }

            comboResetTimer = null;
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

        private void KillBind(CallbackContext input)
        {
            KillBind_ServerRpc();
        }


        [ServerRpc]
        private void KillBind_ServerRpc()
        {
            // await UniTask.WaitForSeconds(3);

            if (!this.IsUnityNull())
            {
                Kill(true);
            }
        } 

        [ClientRpc]
        private void OnSuicide_ClientRpc()
        {
            if (suicideCorpsePrefab != null)
            {
                Destroy(Instantiate(suicideCorpsePrefab, transform.position, transform.rotation), 5);
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