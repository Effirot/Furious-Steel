using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public sealed class SkinBinder : NetworkBehaviour
{
    [SerializeField]
    private Transform SkinOrigin;

    [SerializeField, TextArea]
    private string TargetParentPath = "";

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SkinOrigin.SetParent(transform.parent?.Find(TargetParentPath), false);
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);

        if (parentNetworkObject != null)
        {
            SkinOrigin.SetParent(parentNetworkObject.transform.Find(TargetParentPath), false);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (SkinOrigin != null)
        {
            Destroy(SkinOrigin.gameObject);
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
