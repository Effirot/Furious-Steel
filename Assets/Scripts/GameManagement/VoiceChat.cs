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
    public sealed class VoiceChatConfig 
    {   
        public string microphone = null;
        public float sense = 0.15f;

        internal VoiceChatConfig() { }
    }

    public const int Frequency = 6000;

    public static VoiceChatConfig Config { get; private set; } = new VoiceChatConfig();


    public InputActionReference inputAction;

    public UnityEvent<bool> OnActiveStateChanged = new();

    public bool IsActive {
        get => network_isAcrive.Value;
        private set => network_isAcrive.Value = value;
    }

    private NetworkVariable<bool> network_isAcrive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private AudioSource m_AudioSource;
    private int lastSamplePosition;

    private AudioClip microphoneClip;

    private float[] audioData;

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
            microphoneClip = Microphone.Start(Config.microphone, true, 1, Frequency);
        }
        else
        {
            m_AudioSource.clip = AudioClip.Create("Microphone", Frequency, 1, Frequency, true, PCMReaderCallback);
            m_AudioSource.loop = true;
            m_AudioSource.Play();
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
            float[] samples = new float[microphoneClip.samples];
            var time = Microphone.GetPosition(Config.microphone);
            microphoneClip.GetData(samples, time);
            
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
    }

    [ServerRpc]
    private void SendAudioClipData_ServerRpc(float[] data, ServerRpcParams rpcSendParams = default)
    {
        SendAudioClipData_ClientRpc(data,
            new ClientRpcParams() {
                Send = new () {
                    TargetClientIds = NetworkManager.ConnectedClientsIds.Where(id => id != rpcSendParams.Receive.SenderClientId).ToList()
                }
            }
        );
    }
    [ClientRpc]
    private void SendAudioClipData_ClientRpc(float[] data, ClientRpcParams rpcSendParams = default)
    {
        audioData = data;
    }

    private void PCMReaderCallback(float[] data)
    {
        if (audioData == null) return;

        var arrayLength = Mathf.Min(data.Length, audioData.Length);
        
        for (int i = 0; i < arrayLength; i++)
        {
            data[i] = audioData[i];
            
            if (!IsActive)
            {
                audioData[i] = 0;
            }
        }
    }
}
