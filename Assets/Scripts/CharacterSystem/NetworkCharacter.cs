using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;
using UnityEngine.VFX;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class NetworkCharacter : 
    NetworkBehaviour,
    IDamagable
{
    // [RuntimeInitializeOnLoadMethod]
    // private static void A()
    // {
    //     Application.targetFrameRate = 60;
    // }

    new public Rigidbody rigidbody { get; private set; }

    [field : SerializeField]
    public virtual float Speed { get; set; }

    [field : SerializeField]
    public virtual bool StunlockProtection { get; set; }

    [field : SerializeField]
    public VisualEffect OnHitEffect { get; private set; }
    
    [field : SerializeField]
    public VisualEffect StulockEffect { get; private set; }

    [field : SerializeField]
    private float MaxHealth = 100;

    public float Health { 
        get => network_health.Value; 
        set { 
            if (IsServer)
            {
                network_health.Value = value;
            }
        } 
    }

    [SerializeField]
    private UnityEvent<float> OnHealthChanged = new();

    public Vector2 MovementVector => network_movementVector.Value;
    
    public float Stunlock {
        get => network_stunlock.Value;
        set 
        {
            if (IsServer && !StunlockProtection)
            {
                network_stunlock.Value = value;
            }
        }
    }

    private NetworkVariable<float> network_stunlock = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> network_health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
    private NetworkVariable<Vector2> network_movementVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    protected void SetMovementVector(Vector2 vector)
    {
        if (IsOwner)
        {
            network_movementVector.Value = vector.normalized;
        }
    } 

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            StartCoroutine(StunlockReduceProcess());

            network_health.OnValueChanged += (Old, New) => OnHealthChanged.Invoke(New);
            network_position.Value = rigidbody.position;
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        SwitchOffRigidbodyConstraints();
    }

    protected virtual void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }
    protected virtual void Start()
    {
        
    }

    protected virtual void Update()
    {
        
    }
    protected virtual void FixedUpdate()
    {
        if (Stunlock <= 0 && NetworkManager.Singleton.IsListening)
        {
            CalculateMovement();
            
            RotateCharacter();
        }
    }
    protected virtual void LateUpdate()
    {
        InterpolateToServerPosition();
    }

    protected virtual void Dead()
    {

    }
    protected virtual void Spawn()
    {

    }

    private void CalculateMovement()
    {
        rigidbody.velocity = Vector3.Lerp(
            rigidbody.velocity, 
            new Vector3(
                MovementVector.x * Speed, 
                rigidbody.velocity.y, 
                MovementVector.y * Speed), 
            0.2f
        );
    }
    private void RotateCharacter()
    {
        var lookVector = new Vector3 (MovementVector.x, 0, MovementVector.y);

        if (lookVector.magnitude > 0.1f)
        {
            rigidbody.rotation = Quaternion.Lerp(rigidbody.rotation, Quaternion.LookRotation(lookVector), 0.2f);
        }
    }
    private void InterpolateToServerPosition()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            if(IsServer)
            {
                network_position.Value = rigidbody.position;
            }
            else
            {
                GetComponent<Rigidbody>().position = Vector3.Lerp(rigidbody.position, network_position.Value, 0.3f);
            }
        }
    }

    private IEnumerator StunlockReduceProcess()
    {
        while (true)
        {
            Stunlock = Mathf.Clamp(Stunlock - 0.1f, 0, float.MaxValue);

            if (StulockEffect != null)
            {
                if (Stunlock > 0)
                {
                    StulockEffect.Play();
                }
                else
                {
                    StulockEffect.Stop();
                }
            }
            
            yield return new WaitForSeconds(0.1f);

        }
    }

    protected void SwitchOffRigidbodyConstraints()
    {
        rigidbody.constraints = RigidbodyConstraints.None;
    }

    [ClientRpc]
    private void Spawn_ClientRpc()
    {
        Spawn();
    }
    [ClientRpc]
    private void Dead_ClientRpc()
    {
        Dead();
    }

    public void SendDamage(Damage damage)
    {
        Health -= damage.Value;
        Debug.Log(damage);

        if (Health <= 0 && IsServer)
        {
            Dead_ClientRpc();

            NetworkObject.Despawn(false);
        }
    }
}
