using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using CharacterSystem.Attacks;
using Unity.Collections;



#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacter))]
public class CharacterEffectsHolder : NetworkBehaviour
{
    private class CharacterGlowing
    {
        private int materialInstanceID;
        private SkinnedMeshRenderer renderer;

        public CharacterGlowing (Color color, float power, Shader glowingShader, SkinnedMeshRenderer renderer)
        {
            this.renderer = renderer;

            var material = new Material(glowingShader);

            material.name = material.GetInstanceID().ToString();
            material.renderQueue = 3050;
            material.SetColor("_Color", color);
            
            materialInstanceID = material.GetInstanceID();

            renderer.sharedMaterials = renderer.sharedMaterials.Append(material).ToArray();
        }

        public void Remove()
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Where(material => material.name != materialInstanceID + " (Instance)" ).ToArray();
        
        }
        
    } 

    private struct NetworkCharacterEffectData : 
        INetworkSerializable, 
        IEquatable<NetworkCharacterEffectData>
    {
        public int EffectTypeId;

        public ulong EffectsSourceLink;

        public IDamageSource EffectSource
        {
            get {
                if (EffectsSourceLink != ulong.MinValue)
                {
                    var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

                    if (dictionary.ContainsKey(EffectsSourceLink) && dictionary[EffectsSourceLink].TryGetComponent<IDamageSource>(out var component))
                    {
                        return component;
                    }
                }

                return null;
            }
            set{
                EffectsSourceLink = value?.gameObject?.GetComponent<NetworkObject>()?.NetworkObjectId ?? ulong.MinValue;
            } 
        }
        public Type EffectType
        {
            get {
                return EffectTypeId < 0 || EffectTypeId >= CharacterEffect.AllCharacterEffectTypes.Length ? null : CharacterEffect.AllCharacterEffectTypes[EffectTypeId];
            }
            set{
                EffectTypeId = Array.IndexOf(CharacterEffect.AllCharacterEffectTypes, value);
            } 
        }

        public NetworkCharacterEffectData(int TypeId, ulong EffectsSourceLink)
        {
            this.EffectTypeId = TypeId;
            this.EffectsSourceLink = EffectsSourceLink;
        }
        public NetworkCharacterEffectData(int TypeId, IDamageSource EffectsSource)
        {
            this.EffectTypeId = TypeId;
            this.EffectsSourceLink = 0;
            this.EffectSource = EffectsSource;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref EffectTypeId);
            serializer.SerializeValue(ref EffectsSourceLink);
        }

        public bool Equals(NetworkCharacterEffectData other)
        {
            return EffectTypeId == other.EffectTypeId && EffectsSourceLink == other.EffectsSourceLink;
        }
    }

    [SerializeField]
    private SkinnedMeshRenderer characterSkinnedMeshRenderer;

    [SerializeField]
    private Shader GlowingShader;

    public NetworkCharacter character { get; private set; }

    private NetworkList<NetworkCharacterEffectData> characterEffectIds_network;
    private List<CharacterEffect> characterEffects = new();
    
    private Dictionary<CharacterEffect, List<CharacterGlowing>> characterGlowings = new();

    public bool AddEffect (CharacterEffect effect)
    {
        if (IsServer)
        {
            var dublicate = characterEffects.Find(e => e.GetType() == effect.GetType());
            if (dublicate != null) 
            {
                dublicate.AddDublicate(effect);

                return false;
            }

            effect.effectsHolder = this;

            if (effect.Existance)
            {
                characterEffects.Add(effect);
                
                characterEffectIds_network.Add(new (
                    Array.IndexOf(CharacterEffect.AllCharacterEffectTypes, effect.GetType()),
                    effect.effectsSource));
            
                effect.IsValid = true;
                effect.Start();

                return true;
            }
        }

        return false;
    }

    public void AddGlowing(CharacterEffect bindingEffect, Color color, float power)
    {
        if (!characterGlowings.ContainsKey(bindingEffect))
        {
            characterGlowings.Add(bindingEffect, new());
        }

        characterGlowings[bindingEffect].Add(new CharacterGlowing(color, power, GlowingShader, characterSkinnedMeshRenderer));
    }
    private void RemoveAllGlowings(CharacterEffect bindingEffect)
    {
        if (characterGlowings.ContainsKey(bindingEffect))
        {
            var list = characterGlowings[bindingEffect];
            list.ForEach(a => a.Remove());
            list.Clear();

            characterGlowings.Remove(bindingEffect);
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
        // materialLink = new Material(GlowingShader);
        // characterSkinnedMeshRenderer.materials = characterSkinnedMeshRenderer.materials.Append(materialLink).ToArray();
        // materialLink.SetVector("Color", new Color(0, 0, 0, 0));
        // materialLink.SetFloat("Power", 10);

        // materialLink.renderQueue = 3050;

        character = GetComponent<NetworkCharacter>();

        characterEffectIds_network = new NetworkList<NetworkCharacterEffectData>(Array.Empty<NetworkCharacterEffectData>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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
                    RemoveAllGlowings(characterEffects[i]);

                    characterEffects[i].IsValid = false;
                    characterEffects[i].Remove();
                    characterEffects.RemoveAt(i);
                    
                    characterEffectIds_network.RemoveAt(i);
                    
                    i--;
                }
            }
        }
    }
#if !UNITY_SERVER || UNITY_EDITOR
    private void Update()
    {

    }
#endif

    private void OnListChanged_event (NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData> networkListEvent)
    {
        var index = networkListEvent.Index;

        switch (networkListEvent.Type)
        {
            case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.Add: 
                CharacterEffect instance = Activator.CreateInstance(networkListEvent.Value.EffectType) as CharacterEffect;
                
                instance.effectsHolder = this;
                instance.effectsSource = networkListEvent.Value.EffectSource;
                instance.IsValid = true;
                instance.Start();

                characterEffects.Insert(index, instance);
                break;
                
            case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.Insert: 
                goto case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.Add;

            case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.Remove:
                var effect = characterEffects[index];

                RemoveAllGlowings(effect);
                effect.IsValid = false;
                effect.Remove();
                
                characterEffects.Remove(effect);
                break;

            case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.RemoveAt: 
                goto case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.Remove;

            case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.Value: 
                break;

            case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.Clear: 
                RemoveAll();
                break;

            case NetworkListEvent<CharacterEffectsHolder.NetworkCharacterEffectData>.EventType.Full: 
                RefreshAll();
                break;   
        }
    }

    private void RefreshAll()
    {
        foreach (var item in characterEffectIds_network)
        {
            var type = CharacterEffect.AllCharacterEffectTypes[item.EffectTypeId];
            var instance = Activator.CreateInstance(type) as CharacterEffect;
            instance.effectsHolder = this;
            instance.effectsSource = item.EffectSource;
            instance.IsValid = true;
            instance.Start();

            characterEffects.Add(instance);
        }
    }
    private void RemoveAll()
    {
        foreach (var item in characterEffects)
        {
            RemoveAllGlowings(item);

            item.IsValid = false;
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
            // onListChangedEvent = serializedObject.FindProperty("onListChanged");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // EditorGUILayout.PropertyField(onListChangedEvent);

            foreach (var value in target.characterEffects)
            {
                EditorGUILayout.LabelField(value.ToString());
            }
        }
    }
    #endif
}
