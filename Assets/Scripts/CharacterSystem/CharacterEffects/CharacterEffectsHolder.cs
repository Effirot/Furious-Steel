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

        [SerializeField]
        public SkinnedMeshRenderer characterSkinnedMeshRenderer;

        [SerializeField]
        private Shader GlowingShader;

        public NetworkCharacter character { get; private set; }

        private List<CharacterEffect> characterEffects = new();
        
        private Dictionary<CharacterEffect, CharacterGlowing> characterGlowings = new();

        public bool AddEffect (CharacterEffect effect)
        {
            if (effect != null)
            {
                if (isServer)
                {
                    AddEffect_ClientRpc(effect);
                }

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

                if (effect.Existance || !isServer)
                {
                    characterEffects.Add(effect);
                
                    effect.IsValid = true;
                    effect.Start();
                    
                    return true;
                }
            }

            return false;
        }
        public void RemoveEffect (CharacterEffect effect)
        {
            RemoveEffect(characterEffects.IndexOf(effect));
        }
        public void RemoveEffect (int effectIndex)
        {
            RemoveGlowing(characterEffects[effectIndex]);

            characterEffects[effectIndex].IsValid = false;
            characterEffects[effectIndex].Remove();
            characterEffects.RemoveAt(effectIndex);

            if (isServer)
            {
                RemoveEffect_ClientRpc(effectIndex);
            }
        }

        public void AddGlowing(CharacterEffect bindingEffect, Color color, float power)
        {
#if !UNITY_SERVER || UNITY_EDITOR
            if (!characterGlowings.ContainsKey(bindingEffect))
            {
                characterGlowings.Add(bindingEffect, new CharacterGlowing(color, power, GlowingShader, characterSkinnedMeshRenderer));
            }
#endif
        }
        public void EditGlowing(CharacterEffect bindingEffect, Color color)
        {
#if !UNITY_SERVER || UNITY_EDITOR
            if (characterGlowings.ContainsKey(bindingEffect))
            {
                characterGlowings[bindingEffect].EditColor(color);
            }
#endif
        }
        public void RemoveGlowing(CharacterEffect bindingEffect)
        {
#if !UNITY_SERVER || UNITY_EDITOR
            if (characterGlowings.ContainsKey(bindingEffect))
            {
                characterGlowings[bindingEffect].Remove();
                characterGlowings.Remove(bindingEffect);
            }
#endif
        }

        [ClientRpc]
        private void AddEffect_ClientRpc(CharacterEffect effect)
        {
            if (!isServer)
            {
                AddEffect(effect);
            }
        }
        [ClientRpc]
        private void RemoveEffect_ClientRpc(int effectIndex)
        {
            if (!isServer)
            {
                RemoveEffect(effectIndex);
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

                if (isServer && !characterEffects[i].Existance)
                {
                    RemoveEffect(i);
                    
                    i--;
                }
            }
        }

        #if UNITY_EDITOR
        [CustomEditor(typeof(CharacterEffectsHolder))]
        public class CharacterEffectsHolder_Editor : Editor
        {
            new public CharacterEffectsHolder target => base.target as CharacterEffectsHolder;

            SerializedProperty onListChangedEvent;

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                foreach (var value in target.characterEffects)
                {
                    EditorGUILayout.LabelField(value.ToString());
                }
            }
        }
        #endif
    }
}
