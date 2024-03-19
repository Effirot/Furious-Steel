using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using EventType = Unity.Netcode.NetworkListEvent<int>.EventType;
using NetworkListEvent = Unity.Netcode.NetworkListEvent<int>;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacter))]
public class CharacterEffectsHolder : NetworkBehaviour
{
    private NetworkCharacter character;

    public UnityEvent<NetworkListEvent> onListChanged = new();

    private NetworkList<int> characterEffectIds_network;
    private List<CharacterEffect> characterEffects = new();


    public void AddValue()
    {
        
    }

    public override void OnNetworkSpawn ()
    {
        base.OnNetworkSpawn();

        characterEffectIds_network.OnListChanged += OnListChanged_event;
    }
    public override void OnNetworkDespawn ()
    {
        base.OnNetworkDespawn();

        characterEffectIds_network.OnListChanged -= OnListChanged_event;
    }

    private void OnListChanged_event (NetworkListEvent networkListEvent)
    {
        var index = networkListEvent.Index;

        Type type = null;
        CharacterEffect instance = null;

        switch (networkListEvent.Type)
        {
            case EventType.Add: 
                type = CharacterEffect.AllCharacterEffectTypes[index];
                instance = Activator.CreateInstance(type) as CharacterEffect;
                instance.Start();

                characterEffects.Add(Activator.CreateInstance(type) as CharacterEffect);
                break;
                
            case EventType.Insert: 
                type = CharacterEffect.AllCharacterEffectTypes[index];
                instance = Activator.CreateInstance(type) as CharacterEffect;
                instance.Start();

                characterEffects.Add(Activator.CreateInstance(type) as CharacterEffect);
                break;

            case EventType.Remove:
                characterEffects[index].Remove();
                
                characterEffects.RemoveAt(index);
                break;

            case EventType.RemoveAt: 
                goto case EventType.Remove;

            case EventType.Value: 
                break;

            case EventType.Clear: 
                RemoveAll();
                break;

            case EventType.Full: 
                RefreshAll();
                break;   
        }

        onListChanged.Invoke(networkListEvent);
    }

    private void Awake ()
    {
        characterEffectIds_network = new NetworkList<int>(Array.Empty<int>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    }

    private void RefreshAll()
    {
        foreach (var item in characterEffectIds_network)
        {
            var type = CharacterEffect.AllCharacterEffectTypes[item];
            var instance = Activator.CreateInstance(type) as CharacterEffect;
            instance.Start();

            characterEffects.Add(instance);
        }
    }
    private void RemoveAll()
    {
        foreach (var item in characterEffects)
        {
            item.Remove();
        }
        characterEffects.Clear();
    }
}
