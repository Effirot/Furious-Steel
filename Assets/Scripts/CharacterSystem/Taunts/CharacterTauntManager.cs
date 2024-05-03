

using System.Collections;
using System.Runtime.CompilerServices;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacter))]
public class CharacterTauntManager : NetworkBehaviour
{ 
    public bool IsPlaing => TauntProcessRoutine != null;


    private Animator animator => networkCharacter.animator;
    
    private NetworkCharacter networkCharacter;
    private Coroutine TauntProcessRoutine = null;


    public void PlayTaunt(string tauntName)
    {
        PlayTaunt(tauntName, true);
    }
    public void PlayTaunt(string tauntName, bool forced)
    {
        if (IsPlaing)
        {
            StopCoroutine(TauntProcessRoutine);
            TauntProcessRoutine = null;
        }

        // if (animator.HasState(tauntName))
        // {
        //     TauntProcessRoutine = StartCoroutine(TauntProcess());
        // }

    }

    private void Awake()
    {
        networkCharacter = GetComponent<NetworkCharacter>();
    }

    private IEnumerator TauntProcess()
    {
        yield break;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CharacterTauntManager), true)]
    public class CharacterTauntManager_Editor : Editor
    {
        public new CharacterTauntManager target => base.target as CharacterTauntManager;

        private string tauntName;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            tauntName = GUILayout.TextField(tauntName);

            if (string.IsNullOrEmpty(tauntName))
            {
                if (GUILayout.Button("Play!"))
                {
                    target.PlayTaunt(tauntName, true);
                }
            }
        }
    }
#endif
}