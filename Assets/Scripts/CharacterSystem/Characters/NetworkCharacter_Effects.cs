
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using Unity.VisualScripting;
using System;

#if UNITY_EDITOR
using UnityEditor;
using static UnityEditor.EditorGUILayout;
#endif

namespace CharacterSystem.Objects
{
    public abstract partial class NetworkCharacter : NetworkBehaviour,
        IDamagable,
        ITeammate,
        ISyncedActivitiesSource,
        ITimeScalable,
        IPhysicObject
    {
        [SerializeField]
        private AudioSource OnHitSound;
        [SerializeField]
        private List<VisualEffect> OnHitEffect;
        [SerializeField]
        private UnityEvent<Damage> OnHit = new();

        [SerializeField]
        private AudioSource OnHealSound;
        [SerializeField]
        private List<VisualEffect> OnHealEffect;
        [SerializeField]
        private UnityEvent<Damage> OnHeal = new();

        [SerializeField]
        private UnityEvent<bool> OnStunned = new ();
        [SerializeField]
        private UnityEvent OnJump = new ();
        [SerializeField]
        private UnityEvent<bool> OnGrounded = new ();
        [SerializeField]
        private UnityEvent<DamageDeliveryReport, DamageDeliveryReport> OnWallHit = new ();

        [SerializeField]
        private void OnDamageRecieved_Event(Damage damage)
        {
            if (damage.value >= 0)
            {
                try 
                {
                    OnHit.Invoke(damage);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (damage.type is not Damage.Type.Effect)
                {
                    if (!OnHitSound.IsUnityNull())
                    {
                        OnHitSound.Play();
                    }

                    foreach (var effect in OnHitEffect)
                    {
                        if (effect != null)
                        {
                            if ( effect.HasVector3("Direction"))
                            {
                                effect.SetVector3("Direction", damage.pushDirection);
                            }
                            
                            effect.Play();
                        }
                    }
                }
            }
            else
            {
                try 
                {
                    OnHeal.Invoke(damage);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (OnHealSound != null)
                {
                    OnHealSound.Play();
                }

                foreach (var effect in OnHealEffect)
                {
                    if (effect != null)
                    {
                        effect.Play();
                    }
                }
            }
        }

#if UNITY_EDITOR
        protected partial class NetworkCharacter_Editor : Editor
        {
            private SerializedProperty OnHitSound;
            private SerializedProperty OnHitEffect;
            private SerializedProperty OnHit;
            private SerializedProperty OnHealSound;
            private SerializedProperty OnHealEffect;
            private SerializedProperty OnHeal;
            private SerializedProperty OnStunned;
            private SerializedProperty OnJump;
            private SerializedProperty OnGrounded;
            private SerializedProperty OnWallHit;

            private bool hitFoldState;
            private bool healFoldState;
            private bool otherFoldState;

            public virtual void DrawNetworkCharacterEffects()
            {
                OnHitSound   ??= serializedObject.FindProperty("OnHitSound");
                OnHitEffect  ??= serializedObject.FindProperty("OnHitEffect");
                OnHit        ??= serializedObject.FindProperty("OnHit");
                
                OnHealSound  ??= serializedObject.FindProperty("OnHealSound");
                OnHealEffect ??= serializedObject.FindProperty("OnHealEffect");
                OnHeal       ??= serializedObject.FindProperty("OnHeal");
                
                OnStunned    ??= serializedObject.FindProperty("OnStunned");
                OnJump       ??= serializedObject.FindProperty("OnJump");
                OnGrounded   ??= serializedObject.FindProperty("OnGrounded");
                OnWallHit    ??= serializedObject.FindProperty("OnWallHit");

                if (hitFoldState = EditorGUILayout.Foldout(hitFoldState, "Hit"))
                {
                    EditorGUILayout.PropertyField(OnHitSound);
                    EditorGUILayout.PropertyField(OnHitEffect);
                    EditorGUILayout.PropertyField(OnHit);
                }
                
                if (healFoldState = EditorGUILayout.Foldout(healFoldState, "Heal"))
                {
                    EditorGUILayout.PropertyField(OnHealSound);
                    EditorGUILayout.PropertyField(OnHealEffect);
                    EditorGUILayout.PropertyField(OnHeal);
                }

                if (healFoldState = EditorGUILayout.Foldout(healFoldState, "Other"))
                {
                    EditorGUILayout.PropertyField(OnStunned);
                    EditorGUILayout.PropertyField(OnJump);
                    EditorGUILayout.PropertyField(OnGrounded);
                    EditorGUILayout.PropertyField(OnWallHit);

                    
                }

                serializedObject.ApplyModifiedProperties();                
            }
        }
#endif
    }
}