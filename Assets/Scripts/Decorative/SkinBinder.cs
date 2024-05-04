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
            origin.rootBone = parentNetworkObject.transform.Find(TargetParentPath);
            origin.bones = origin.rootBone.parent.GetComponentsInChildren<Transform>();
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
