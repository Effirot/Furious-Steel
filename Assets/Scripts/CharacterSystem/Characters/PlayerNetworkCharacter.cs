using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using CharacterSystem.PowerUps;
using Cysharp.Threading.Tasks;
using Effiry.Items;
using Unity.Cinemachine;
using Unity.Collections;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.XInput;
using UnityEngine.VFX;
using static UnityEngine.InputSystem.InputAction;
using kcp2k;

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

        public DamageBlocker Blocker { get; set; }
        public DamageDeliveryReport lastReport { get; set; } = new();

        [HideInInspector, SyncVar]
        public bool JumpButtonState = false;

        public event Action<DamageDeliveryReport> onDamageDelivered;
        public event Action<int> onComboChanged;
        public UnityEvent<PowerUp> onPowerUpChanged { get; } = new();

        int IDamageSource.Combo => combo;
        int IPowerUpActivator.PowerUpId { get => powerUpId; set => powerUpId = value; }

        [SyncVar(hook = nameof(OnComboChanged))]
        protected int combo;
        
        [SyncVar(hook = nameof(OnPowerUpChanged))]
        private int powerUpId = -1;

        private Coroutine comboResetTimer = null;

        [HideInInspector]
        public Vector2 internalMovementVector = Vector2.zero;
        [HideInInspector]
        public Vector2 internalLookVector = Vector2.zero;


        public virtual NetworkIdentity SetWeapon (Item item, NetworkConnectionToClient connectionToClient)
        {
            if (item == null)
                return null;

            var weaponPrefab = RoomManager.Singleton.ResearchWeaponPrefab(item.TypeName);

            if (weaponPrefab == null)
                return null;

            var weaponGameObject = Instantiate(weaponPrefab, transform);
            var identity = weaponGameObject.GetComponent<NetworkIdentity>();
            
            weaponGameObject.transform.SetParent(transform);
            NetworkServer.Spawn(weaponGameObject, connectionToClient);
            identity.AssignClientAuthority(connectionToClient);
            
            if (weaponGameObject.TryGetComponent<ItemBinder>(out var component))
            {
                component.item = item;
            }    

            return identity;
        } 
        public virtual NetworkIdentity SetTrinket (Item item)
        {
            if (item == null)
                return null;

            var trinket = RoomManager.Singleton.ResearchTrinketPrefab(item.TypeName);

            if (trinket == null)
                return null;

            var trinketGameObject = Instantiate(trinket, transform);
            var identity = trinketGameObject.GetComponent<NetworkIdentity>();
            
            trinketGameObject.transform.SetParent(transform);
            NetworkServer.Spawn(trinketGameObject, connectionToClient);
            identity.AssignClientAuthority(connectionToClient);
            
            SyncNetworkIdentityParent(netIdentity, identity, false);

            if (trinketGameObject.TryGetComponent<ItemBinder>(out var component))
            {
                component.item = item;
            }    

            return trinketGameObject.GetComponent<NetworkIdentity>();
        }

        public override bool Hit(Damage damage)
        {
            var isBlocked = Blocker != null && Blocker.Block(ref damage) || base.Hit(damage);

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
                        combo = Mathf.Min(combo + 1, 30);
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

            if (isLocalPlayer)
            {
                Owner = this;
                
                CharacterCameraObserver.Singleton.ObservingObject = this;

                if (moveInput != null) {
                    var action = moveInput.action;
                    action.Enable();

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

            UpdateName();

            RoomManager.Singleton.playersData.OnChange += OnOwnerPlayerDataChanged_event;
            
            Players.Add(this);
        }
        public override void OnStopClient()
        {
            base.OnStopClient();

            RoomManager.Singleton.playersData.OnChange -= OnOwnerPlayerDataChanged_event;

            Players.Remove(this);

            if (isLocalPlayer)
            {
                Owner = null;
                
                {
                    var action = moveInput.action;
                    
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
        }

        protected override void OnHitReaction(Damage damage)
        {
            base.OnHitReaction(damage);

            if (isLocalPlayer && damage.type is not Damage.Type.Effect)
            {
                OnHitImpulseSource?.GenerateImpulse(damage.value / 8f);
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

        protected override void Dead()
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

            if (isLocalPlayer && !corpseObject.IsUnityNull() && corpseObject.TryGetComponent<IObservableObject>(out var observableObject))
            {
                CharacterCameraObserver.Singleton.ObservingObject = observableObject;
            }

            return corpseObject;
        }


        protected virtual void OnPowerUpChanged(int Old, int New)
        {
            onPowerUpChanged.Invoke(PowerUp.IdToPowerUpLink(New));
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

            onComboChanged.Invoke(New);

            Speed += (New - Old) / 12f;
        }

        private void OnOwnerPlayerDataChanged_event(SyncList<RoomManager.PublicClientData>.Operation operation, int index, RoomManager.PublicClientData value)
        {
            UpdateName();
        }
        private void UpdateName()
        {
#warning fix nicknames
            // gameObject.name = name = ClientData.Name.Value;
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

        [Command, Server]
        private void SyncNetworkIdentityParent(NetworkIdentity parent, NetworkIdentity child, bool worldPositionStays)
        {
            child.transform.SetParent(parent.transform, worldPositionStays);
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
                if (suicideCorpsePrefab != null)
                {
                    var corpse = Instantiate(suicideCorpsePrefab, transform.position, transform.rotation);
                    
                    corpse.SetActive(true); 
                    
                    Destroy(corpse, 5);
                }
            
                Damage.Deliver(this, new Damage(maxHealth, null, 100, Vector3.up, Damage.Type.Unblockable));
            }
        } 
    }
}