using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public sealed class SkinBinder : NetworkBehaviour
{
    [SerializeField, TextArea]
    private string TargetParentPath = "";

    [SerializeField]
    private SkinnedMeshRenderer origin;

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);
        
        if (parentNetworkObject != null)
        {
            Research(parentNetworkObject.gameObject);
        }
    }

    private void Start()
    {
        Research(transform.parent.gameObject);
    }

    private void Research(GameObject target)
    {
        if (target != null)
        {
            origin.rootBone = target.transform.Find(TargetParentPath);
            
            if (origin.rootBone != null)
            {
                origin.bones = origin.rootBone.parent.parent.GetComponentInChildren<SkinnedMeshRenderer>().bones;

                // Debug.Log(string.Join("\n", origin.bones.Select(a=>a.gameObject.name)));
                // Debug.Log(string.Join("\n", EnumerateTransforms(origin.rootBone).Select(a=>a.gameObject.name)));
            }
        }
    }

    private IEnumerable<Transform> EnumerateTransforms(Transform transform, bool reversed = false)
    {
        yield return transform;
        
        if (reversed)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                foreach (var child in EnumerateTransforms(transform.GetChild(i), true))
                {
                    yield return child;
                }
            }
        }
        else
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                foreach (var child in EnumerateTransforms(transform.GetChild(i), true))
                {
                    yield return child;
                }
            }
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(SkinBinder))]
    public class SkinBinder_Editor : Editor
    {
        new private SkinBinder target => base.target as SkinBinder;

        private Transform researchTarget = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
    // /Armor_Armature/SpineCenter/Spine.001/Spine.002/Shoulder.L/Hand.001.L/Hand.002.L/Palm.L
            if (GUILayout.Button("Research"))
            {
                researchTarget = target.transform.parent.Find(target.TargetParentPath);
            }

            if (researchTarget != null)
            {
                GUILayout.Label($"Finded! {researchTarget.gameObject.name}"); 
            }
            else
            {
                GUILayout.Label("None");
            }
        }
    }

#endif
}
