using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
[DisallowMultipleComponent]
public sealed class SkinBinder : NetworkBehaviour
{
    [SerializeField, TextArea]
    private string TargetParentPath = "";

    private SkinnedMeshRenderer origin;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        origin.rootBone = transform.parent?.Find(TargetParentPath);
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);

        origin.rootBone = parentNetworkObject?.transform.Find(TargetParentPath);
    }

    private void Awake()
    {
        origin = GetComponent<SkinnedMeshRenderer>();
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
