using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using EventType = Unity.Netcode.NetworkListEvent<int>.EventType;
using NetworkListEvent = Unity.Netcode.NetworkListEvent<int>;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacter))]
public class CharacterEffectsHolder : NetworkBehaviour
{
    public NetworkCharacter character { get; private set; }

    public UnityEvent<NetworkListEvent> onListChanged = new();

    private NetworkList<int> characterEffectIds_network;
    private List<CharacterEffect> characterEffects = new();


    public void AddEffect (CharacterEffect characterEffect)
    {
        if (IsServer)
        {
            characterEffect.effectsHolder = this;

            characterEffects.Add(characterEffect);
            
            characterEffectIds_network.Add(Array.IndexOf(CharacterEffect.AllCharacterEffectTypes, characterEffect.GetType()));
        
            characterEffect.Start();
        }
    }

    public override void OnNetworkSpawn ()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            characterEffectIds_network.OnListChanged += OnListChanged_event;
        }
    }
    public override void OnNetworkDespawn ()
    {
        base.OnNetworkDespawn();

        if (!IsServer)
        {
            characterEffectIds_network.OnListChanged -= OnListChanged_event;
        }
    }

    private void Awake ()
    {
        character = GetComponent<NetworkCharacter>();

        characterEffectIds_network = new NetworkList<int>(Array.Empty<int>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }
    private void FixedUpdate()
    {
        for (int i = 0; i < characterEffects.Count(); i++)
        {
            characterEffects[i].Update();

            if (IsServer)
            {
                if (!characterEffects[i].Existance)
                {
                    characterEffects[i].Remove();
                    characterEffects.RemoveAt(i);
                    
                    characterEffectIds_network.RemoveAt(i);
                    
                    i--;
                }
            }
        }
    }

    private void OnListChanged_event (NetworkListEvent networkListEvent)
    {
        var index = networkListEvent.Index;

        switch (networkListEvent.Type)
        {
            case EventType.Add: 
                CharacterEffect instance = Activator.CreateInstance(CharacterEffect.AllCharacterEffectTypes[index]) as CharacterEffect;
                
                instance.effectsHolder = this;
                instance.Start();

                characterEffects.Insert(index, instance);
                break;
                
            case EventType.Insert: 
                goto case EventType.Add;

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

    #if UNITY_EDITOR
    [CustomEditor(typeof(CharacterEffectsHolder))]
    public class CharacterEffectsHolder_Editor : Editor
    {
        new public CharacterEffectsHolder target => base.target as CharacterEffectsHolder;

        SerializedProperty onListChangedEvent;

        void OnEnable()
        {
            onListChangedEvent = serializedObject.FindProperty("onListChanged");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(onListChangedEvent);

            foreach (var value in target.characterEffects)
            {
                EditorGUILayout.LabelField(value.ToString());
            }
        }
    }
    #endif
}
