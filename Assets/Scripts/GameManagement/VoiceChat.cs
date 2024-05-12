using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class VoiceChat : NetworkBehaviour
{
    public const int Frequency = 6000;

    public InputActionReference inputAction;

    public UnityEvent<bool> OnActiveStateChanged = new();

    public bool IsActive {
        get => network_isAcrive.Value;
        private set => network_isAcrive.Value = value;
    }

    private NetworkVariable<bool> network_isAcrive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private AudioSource m_AudioSource;
    private int lastSamplePosition;

    public override void OnNetworkSpawn()
    {
        m_AudioSource = GetComponent<AudioSource>();

        base.OnNetworkSpawn();

        network_isAcrive.OnValueChanged += OnActiveStateChanged_Event ;

        if (IsOwner && inputAction != null)
        {
            inputAction.action.canceled += OnInputActionChanged_Event;
            inputAction.action.performed += OnInputActionChanged_Event;
        }

        if (IsOwner)
        {
            m_AudioSource.clip = Microphone.Start(Microphone.devices[0], true, 1, Frequency);
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner && inputAction != null)
        {
            inputAction.action.canceled -= OnInputActionChanged_Event;
            inputAction.action.performed -= OnInputActionChanged_Event;
        }

        if (IsOwner)
        {
            Microphone.End(Microphone.devices[0]);
        }
    }

    public void FixedUpdate()
    {
        if (IsActive && IsOwner)
        {
            float[] samples = new float[m_AudioSource.clip.samples * m_AudioSource.clip.channels];
                
            SendAudioClipData_ServerRpc(samples);
        }
    }

    private void OnInputActionChanged_Event(CallbackContext callbackContext)
    {
        IsActive = callbackContext.ReadValueAsButton();
    }
    private void OnActiveStateChanged_Event(bool OldValue, bool NewValue)
    {
        OnActiveStateChanged.Invoke(NewValue);

        if (!NewValue && !IsOwner)
        {
            m_AudioSource.Stop();
        }
    }

    [ServerRpc]
    private void SendAudioClipData_ServerRpc(float[] data, ServerRpcParams rpcSendParams = default)
    {
        SendAudioClipData_ClientRpc(data, new ClientRpcParams() {
            Send = new () {
                TargetClientIds = NetworkManager.ConnectedClientsIds.Where(id => id != rpcSendParams.Receive.SenderClientId).ToList()
            }
        });
    }
    [ClientRpc]
    private void SendAudioClipData_ClientRpc(float[] data, ClientRpcParams rpcSendParams = default)
    {
        PlayAudioClipDataLocaly(data);
    }


    private void PlayAudioClipDataLocaly(float[] data)
    {
        m_AudioSource.clip ??= AudioClip.Create("Microphone", 512, 1, Frequency, false);

        m_AudioSource.clip.SetData(data, 0);
        
        if (!m_AudioSource.isPlaying)
        {
            m_AudioSource.timeSamples = 0;
            m_AudioSource.Play();
        } 
    }
}
