using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

// WinBlocker HAHAHAHA

namespace CharacterSystem.Blocking
{
    public interface IDamageBlocker : 
        ISyncedActivitiesSource, 
        IDamagable,
        IDamageSource
    {
        DamageBlocker Blocker { set; }

        public event Action<bool> onStunStateChanged;
    }

    public class DamageBlocker : SyncedActivity<IDamageBlocker>
    {
        [SerializeField]
        private bool IsActiveAsDefault = true;

        [Space]
        [Header("Stun")]
        [SerializeField]
        private CharacterPermission BeforeBlockCharacterPermissions = CharacterPermission.None;
        [SerializeField, Range(0f, 5f)]
        private float BeforeBlockTime = 0.5f;
        
        [SerializeField]
        private CharacterPermission BlockCharacterPermissions = CharacterPermission.None;
        [SerializeField, Range(0f, 5f)]
        private float BlockProcessTime = 0.5f;
        [SerializeField]
        private bool HoldMode = false;

        [SerializeField]
        private CharacterPermission AfterBlockCharacterPermissions = CharacterPermission.None;
        [SerializeField, Range(0f, 5f)]
        private float AfterBlockTime = 0.5f;

        [Space]
        [SerializeField, Range(0f, 9f)]
        private float PushForce = 0.5f;

        [SerializeField]
        private bool InterruptOnHit = true;

        [Space]
        [Header("Damage")]
        [SerializeField]
        private Damage backDamage;

        [SerializeField, Range(0, 1)]
        public float DamageReducing = 1;

        [Space]
        [Header("Events")]
        [SerializeField]
        private UnityEvent OnBeforeBlockingEvent = new();
        [SerializeField]
        private UnityEvent OnAfterBlockingEvent = new();
        [SerializeField]
        private UnityEvent OnBlockingEvent = new();
        [SerializeField]
        private UnityEvent OnSuccesfulBlockingEvent = new();

        public bool IsPerforming {
            get => network_isPerforming.Value;
            set {
                if (IsServer)
                {
                    network_isPerforming.Value = value;
                }
            }
        }

        public bool IsBlockActive { get; private set; }

        private NetworkVariable<bool> network_isPerforming = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            network_isPerforming.Value = IsActiveAsDefault;
        }

        public virtual bool Block(ref Damage damage)
        {            
            if (damage.type == Damage.Type.Parrying || damage.type == Damage.Type.Unblockable || damage.type == Damage.Type.Effect) 
                return false;

            if (IsBlockActive)
            {
                if (IsServer)
                {
                    ExecuteSuccesfullyBlockEvent_ClientRpc();

                    if (!IsClient)
                    {
                        OnSuccesfulBlockingEvent.Invoke();
                    }
                }

                if (damage.sender != null && damage.type != Damage.Type.Magical && damage.type != Damage.Type.Balistic)
                {                    
                    backDamage.sender = Invoker;
                    backDamage.pushDirection = Vector3.up * 0.3f + -damage.pushDirection.normalized * PushForce;
                    backDamage.type = Damage.Type.Parrying;

                    Damage.Deliver(damage.sender, backDamage);
                }
                damage *= 1f - DamageReducing;

                if (InterruptOnHit)
                {
                    Stop();
                }

                return true;
            }

            return false;
        } 

        public override void Play()
        {
            if (!Invoker.permissions.HasFlag(CharacterPermission.AllowBlocking))
                return;

            if (Invoker.isStunned || !IsPerforming)
                return;

            base.Play();
        }
        public override void Stop()
        {
            if (IsInProcess)
            {                                
                Permissions = CharacterPermission.Default;
            
                Invoker.animator.SetBool("Blocking", false);
                IsBlockActive = false;
                
                base.Stop();
            }
        }
        
        public override IEnumerator Process()
        {
            Permissions = BeforeBlockCharacterPermissions;
            Invoker.Blocker = this;
            Invoker.Push(Vector3.up * 0.01f);
            
            var array = Invoker.activities;
            for (int i = 0; i < array.Count(); i++)
            {
                if (array[i] is DamageSource)
                {
                    array[i].Stop();
                }
            }

            OnBeforeBlockingEvent.Invoke();

            yield return new WaitForSeconds(BeforeBlockTime);

            IsBlockActive = true;
            Permissions = BlockCharacterPermissions;
            OnBlockingEvent.Invoke();

            Invoker.animator.SetBool("Blocking", true);

            if (HoldMode)
            {
                var yield = new WaitForFixedUpdate();    

                while (IsPressed)
                {
                    yield return yield;    
                }
            }
            else
            {
                yield return new WaitForSeconds(BlockProcessTime);
            }

            Invoker.animator.SetBool("Blocking", false);


            IsBlockActive = false;
            Permissions = AfterBlockCharacterPermissions;
            OnAfterBlockingEvent.Invoke();

            yield return new WaitForSeconds(AfterBlockTime);
        }

        [ClientRpc]
        private void ExecuteSuccesfullyBlockEvent_ClientRpc()
        {
            OnSuccesfulBlockingEvent.Invoke();
        }
    }
}
