using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using CharacterSystem.Attacks;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using Mirror;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CharacterSystem.Effects
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkCharacter))]
    public class CharacterEffectsHolder : NetworkBehaviour
    {
        private class CharacterGlowing
        {
            private int materialInstanceID;
            private SkinnedMeshRenderer renderer;

            private Material material {
                get {
                    return Array.Find(renderer.sharedMaterials, material => material.name == materialInstanceID + " (Instance)");
                }
            }

            public CharacterGlowing (Color color, float power, Shader glowingShader, SkinnedMeshRenderer renderer)
            {
                this.renderer = renderer;

                var material = new Material(glowingShader);

                material.name = material.GetInstanceID().ToString();
                material.renderQueue = 3050;
                material.SetColor("_Color", color);
                material.SetFloat("_Power", power);
                
                materialInstanceID = material.GetInstanceID();

                renderer.sharedMaterials = renderer.sharedMaterials.Append(material).ToArray();
            }

            public void EditColor (Color color)
            {
                material?.SetColor("_Color", color);
            }
            public void EditPower (float power)
            {
                material?.SetFloat("_Power", power);
            }

            public async void Remove()
            {
                var materialLink = material;
                var color = materialLink.GetColor("_Color");

                while (materialLink != null && color.a > 0.05f)
                {
                    color.a -= Time.deltaTime * 10; 

                    materialLink.SetColor("_Color", color);

                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
                }

                if (renderer != null)
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Where(material => material.name != materialInstanceID + " (Instance)" ).ToArray();
                }
            }
        } 

        private struct NetworkCharacterEffectData : IEquatable<NetworkCharacterEffectData>
        {
            public int EffectTypeId;

            public uint EffectsSourceLink;

            public IDamageSource EffectSource
            {
                get {

                    var dictionary = NetworkServer.spawned;

                    if (dictionary.ContainsKey(EffectsSourceLink) && dictionary[EffectsSourceLink].TryGetComponent<IDamageSource>(out var component))
                    {
                        return component;
                    }
                    
                    return null;
                }
                set {
                    EffectsSourceLink = value?.gameObject?.GetComponent<NetworkIdentity>()?.netId ?? uint.MinValue;
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

            public NetworkCharacterEffectData(int TypeId, uint EffectsSourceLink)
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

            public bool Equals(NetworkCharacterEffectData other)
            {
                return EffectTypeId == other.EffectTypeId && EffectsSourceLink == other.EffectsSourceLink;
            }
        }

        [SerializeField]
        public SkinnedMeshRenderer characterSkinnedMeshRenderer;

        [SerializeField]
        private Shader GlowingShader;

        public NetworkCharacter character { get; private set; }

        private SyncList<NetworkCharacterEffectData> characterEffectIds_network = new ();
        private List<CharacterEffect> characterEffects = new();
        
        private Dictionary<CharacterEffect, CharacterGlowing> characterGlowings = new();

        public bool AddEffect (CharacterEffect effect)
        {
            if (isServer)
            {
                var dublicate = characterEffects.Find(e => e.GetType() == effect.GetType());
                if (dublicate != null) 
                {
                    if (effect.effectsSource != null)
                    {
                        dublicate.effectsSource = effect.effectsSource;
                    }

                    dublicate.AddDublicate(effect);

                    return false;
                }

                effect.effectsHolder = this;

                if (effect.Existance)
                {
                    characterEffects.Add(effect);
                    
                    characterEffectIds_network.Add(new (
                        effect.indexOfEffect,
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
                characterGlowings.Add(bindingEffect, new CharacterGlowing(color, power, GlowingShader, characterSkinnedMeshRenderer));
            }
        }
        public void EditGlowing(CharacterEffect bindingEffect, Color color)
        {
            if (characterGlowings.ContainsKey(bindingEffect))
            {
                characterGlowings[bindingEffect].EditColor(color);
            }
        }
        public void RemoveGlowing(CharacterEffect bindingEffect)
        {
            if (characterGlowings.ContainsKey(bindingEffect))
            {
                characterGlowings[bindingEffect].Remove();
                characterGlowings.Remove(bindingEffect);
            }
        }


        private void Start ()
        {
            if (!isServer)
            {
                characterEffectIds_network.OnChange += OnListChanged_event;
            }
        }
        private void OnDestroy ()
        {
            if (!isServer)
            {
                characterEffectIds_network.OnChange -= OnListChanged_event;
            }
        }

        private void Awake ()
        {
            character = GetComponent<NetworkCharacter>();
        }
        private void FixedUpdate()
        {
            for (int i = 0; i < characterEffects.Count(); i++)
            {
                characterEffects[i].Update();

                if (isServer)
                {
                    if (!characterEffects[i].Existance)
                    {
                        RemoveGlowing(characterEffects[i]);

                        characterEffects[i].IsValid = false;
                        characterEffects[i].Remove();
                        characterEffects.RemoveAt(i);
                        
                        characterEffectIds_network.RemoveAt(i);
                        
                        i--;
                    }
                }
            }
        }

        private void OnListChanged_event (SyncList<NetworkCharacterEffectData>.Operation operation, int index, NetworkCharacterEffectData networkCharacterEffectData)
        {
            switch (operation)
            {
                case SyncList<NetworkCharacterEffectData>.Operation.OP_ADD: 
                    CharacterEffect instance = Activator.CreateInstance(networkCharacterEffectData.EffectType) as CharacterEffect;
                    
                    instance.effectsHolder = this;
                    instance.effectsSource = networkCharacterEffectData.EffectSource;
                    instance.IsValid = true;
                    instance.Start();

                    characterEffects.Insert(index, instance);
                    break;

                case SyncList<NetworkCharacterEffectData>.Operation.OP_INSERT:
                    goto case SyncList<NetworkCharacterEffectData>.Operation.OP_ADD;

                case SyncList<NetworkCharacterEffectData>.Operation.OP_REMOVEAT:
                    var effect = characterEffects[index];

                    RemoveGlowing(effect);
                    effect.IsValid = false;
                    effect.Remove();
                    
                    characterEffects.Remove(effect);
                    break;

                case SyncList<NetworkCharacterEffectData>.Operation.OP_SET: 
                    break;

                case SyncList<NetworkCharacterEffectData>.Operation.OP_CLEAR: 
                    RemoveAll();
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
                RemoveGlowing(item);

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
}
