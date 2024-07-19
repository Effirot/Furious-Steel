
using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using CharacterSystem.Interactions;
using CharacterSystem.PowerUps;
using Effiry.Items;
using Mirror;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;
using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Objects
{
    public class PlayerNetworkCharacter : NetworkCharacter,
        IDamageSource,
        IDamageBlocker,
        IPowerUpActivator,
        IObservableObject,
        IThrower
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

        public DamageBlocker Blocker { get; set; }
        public DamageDeliveryReport lastReport { get; set; } = new();

        [HideInInspector, SyncVar]
        public bool JumpButtonState = false;

        public event Action<DamageDeliveryReport> onDamageDelivered;
        public event Action<int> onComboChanged;
        public UnityEvent<PowerUp> onPowerUpChanged { get; } = new();

        int IDamageSource.Combo => combo;
        float IDamageSource.DamageMultipliyer { get => DamageMultipliyer; set => DamageMultipliyer = value; }
        int IPowerUpActivator.PowerUpId { get => powerUpId; set => powerUpId = value; }

        public ConnectedPlayerData Data => ConnectedPlayerData.All.Find(data => data.netIdentity.connectionToClient == netIdentity.connectionToClient);


        [SyncVar(hook = nameof(OnComboChanged))]
        protected int combo;

        [SyncVar(/* hook = nameof(OnDamageMultipliyerChanged) */)]
        public float DamageMultipliyer = 1f;
        
        [SyncVar(hook = nameof(OnPowerUpChanged))]
        private int powerUpId = -1;
        
        [SyncVar(hook = nameof(OnWeaponChanged))]
        private NetworkIdentity weaponIdentity;
        [SyncVar(hook = nameof(OnTrinketChanged))]
        private NetworkIdentity trinketIdentity;

        private Coroutine comboResetTimer = null;

        public Vector2 internalMovementVector { get; set; } = Vector2.zero;
        public Vector2 internalLookVector { get; set; } = Vector2.zero;
        public Transform pickPoint { get; set; }

        public virtual NetworkIdentity SetWeapon (Item item)
        {
            if (item == null)
                return null;

            var weaponPrefab = RoomManager.Singleton.ResearchWeaponPrefab(item.TypeName);

            if (weaponPrefab == null)
                return null;

            var weaponGameObject = Instantiate(weaponPrefab, transform);
            if (!weaponGameObject.TryGetComponent<NetworkIdentity>(out weaponIdentity))
            {
                Debug.LogError($"Weapon {weaponGameObject.name} NetworkIdentity was not founded");
            }
            else
            {
                if (isServerOnly)
                {
                    OnWeaponChanged(null, weaponIdentity);
                }
            }

            weaponGameObject.transform.SetParent(transform);
            NetworkServer.Spawn(weaponGameObject, connectionToClient);
            weaponIdentity.AssignClientAuthority(connectionToClient);
            
            if (weaponGameObject.TryGetComponent<ItemBinder>(out var component))
            {
                component.item = item;
            }

            return weaponIdentity;
        } 
        public virtual NetworkIdentity SetTrinket (Item item)
        {
            if (item == null)
                return null;

            var trinketPrefab = RoomManager.Singleton.ResearchTrinketPrefab(item.TypeName);

            if (trinketPrefab == null)
                return null;

            var trinketGameObject = Instantiate(trinketPrefab, transform);
            if (!trinketGameObject.TryGetComponent<NetworkIdentity>(out trinketIdentity))
            {
                Debug.LogError($"Weapon {trinketGameObject.name} NetworkIdentity was not founded");
            }
            else
            {
                if (isServerOnly)
                {
                    OnWeaponChanged(null, trinketIdentity);
                }
            }
            
            trinketGameObject.transform.SetParent(transform);
            NetworkServer.Spawn(trinketGameObject, connectionToClient);
            trinketIdentity.AssignClientAuthority(connectionToClient);
            
            if (trinketGameObject.TryGetComponent<ItemBinder>(out var component))
            {
                component.item = item;
            }

            return trinketIdentity;
        }

        public override bool Hit(Damage damage)
        {
            var isBlocked = Blocker != null && Blocker.CheckBlocking(ref damage) || base.Hit(damage);

            if (isBlocked)
            {                
                combo += 10;               
            }

            return isBlocked;
        }
        public virtual void DamageDelivered(DamageDeliveryReport report)
        {
            if (isServer && report.isDelivered)
            {            
                if (report.damage.type is not Damage.Type.Effect)
                {
                    if (report.isBlocked)
                    {
                        combo = 0;
                    }
                    else
                    {
                        if (report.target is NetworkCharacter)
                        {
                            var newCombo = Mathf.Clamp(combo + 1, 0, 30);
                            
                            if (isServerOnly)
                            {
                                OnComboChanged(combo, newCombo);
                            }
                            
                            combo = newCombo;
                        }
                    }
                }
                
                onDamageDelivered?.Invoke(report);
            }
            
            if (isLocalPlayer && report.damage.type is not Damage.Type.Effect)
            {
                OnHitImpulseSource?.GenerateImpulse(report.damage.value / 10f);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            gameObject.name = name = Data?.Name ?? "";
        }
        public override void OnStopClient()
        {
            base.OnStopClient();
        }
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            Owner = this;
            
            CharacterCameraObserver.Singleton.ObservingObject = this;

            if (moveInput != null) {
                var action = moveInput.action;
                action.Enable();

                action.started += OnMoveInput;
                action.performed += OnMoveInput;
                action.canceled += OnMoveInput;
            }

            if (lookInput != null) {
                var action = lookInput.action;
                action.Enable();

                action.performed += OnLookInput;
                action.canceled += OnLookInput;
            }

            if (jumpInput != null) {
                var action = jumpInput.action;
                action.Enable();

                action.performed += OnJumpInput;
                action.canceled += OnJumpInput;
                                    
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
        public override void OnStopLocalPlayer()
        {
            base.OnStopLocalPlayer();

            Owner = null;
            
            {
                var action = moveInput.action;
                
                action.started -= OnMoveInput;
                action.performed -= OnMoveInput;
                action.canceled -= OnMoveInput;
            }
            
            {
                var action = lookInput.action;

                action.performed -= OnLookInput;
                action.canceled -= OnLookInput;
            }

            {
                var action = jumpInput.action;

                action.performed -= OnJumpInput;
                action.canceled -= OnJumpInput;
            }

            {
                var action = killBindInput.action;

                action.performed -= KillBind;
                action.canceled -= KillBind;
            }
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            
            Players.Add(this);
        }
        public override void OnStopServer()
        {
            base.OnStopServer();

            Players.Remove(this);
        }

        protected override void OnHitReaction(Damage damage)
        {
            base.OnHitReaction(damage);

            if (isLocalPlayer && damage.type is not Damage.Type.Effect)
            {
                OnHitImpulseSource?.GenerateImpulse(Mathf.Min(damage.value / 8f, 4f));
            }
        }
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isLocalPlayer)
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
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            var lookPoint = new Vector3(internalLookVector.x, 0, internalLookVector.y) + transform.position;

            Gizmos.DrawWireSphere(lookPoint, 0.2f);
        }

        protected override void Dead(Damage damage)
        {
            OnPlayerCharacterDead.Invoke(this);

            if (isLocalPlayer)
            {
                OnOwnerPlayerCharacterDead.Invoke(this);
            }
        }
        protected override void Spawn()
        {
            base.Spawn();

            OnPlayerCharacterSpawn.Invoke(this);
        }

        protected override GameObject SpawnCorpse()
        {
            var corpseObject = base.SpawnCorpse();

            if (isLocalPlayer && corpseObject.TryGetComponent<IObservableObject>(out var observableObject))
            {
                CharacterCameraObserver.Singleton.ObservingObject = observableObject;
            }

            return corpseObject;
        }

        protected virtual void OnWeaponChanged(NetworkIdentity Old, NetworkIdentity New)
        {

        }
        protected virtual void OnTrinketChanged(NetworkIdentity Old, NetworkIdentity New)
        {

        }
        protected virtual void OnPowerUpChanged(int Old, int New)
        {
            onPowerUpChanged?.Invoke(PowerUp.IdToPowerUpLink(New));
        }
        protected virtual void OnComboChanged(int Old, int New)
        {
            if (isServer)
            {
                if (New > Old)
                {
                    if (comboResetTimer != null && !this.IsUnityNull())
                    {
                        StopCoroutine(comboResetTimer);
                        comboResetTimer = null;
                    }

                    comboResetTimer = StartCoroutine(ComboResetTimer());
                }
            }

            onComboChanged?.Invoke(New);

            Speed += (New - Old) / 12f;
        }

        private IEnumerator ComboResetTimer()
        {
            yield return new WaitForSeconds(1f);

            var reduceTimeout = 0.6f;

            while (combo > 0)
            {
                yield return new WaitForSeconds(reduceTimeout);

                reduceTimeout *= 0.92f;    
                combo -= 1;
            }

            combo = 0;

            comboResetTimer = null;
        }

        private void OnMoveInput(CallbackContext input)
        {
            internalMovementVector = input.ReadValue<Vector2>();
        }
        private void OnLookInput(CallbackContext input)
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
        private void OnJumpInput(CallbackContext input)
        {
            var state = input.ReadValueAsButton();

            SetJumpState(state);

            if (state)
            {
                Jump(movementVector);
            }
        }

        [Client, Command]
        private void SetJumpState(bool Value)
        {
            JumpButtonState = Value;
        }

        [Client, Command]
        private void KillBind(CallbackContext input)
        {
            if (!this.IsUnityNull())
            {
                Damage.Deliver(this, new Damage(maxHealth * 100, null, 100, Vector3.up, Damage.Type.Unblockable));
            }
        } 
    }
}