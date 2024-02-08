using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PowerUpContainer : NetworkBehaviour
{
    [field : SerializeField]
    public int Id { get; set; }

    public PowerUp powerUp => Id < 0 || Id >= PowerUp.AllPowerUps.Length ? null : PowerUp.AllPowerUps[Id];

#if UNITY_EDITOR

    [CustomEditor(typeof(PowerUpContainer), true)]
    public class PowerUpContainer_Editor : Editor
    {
        new private PowerUpContainer target => base.target as PowerUpContainer;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Label(target.powerUp?.GetType().Name ?? "None");
        }
    }

#endif
}