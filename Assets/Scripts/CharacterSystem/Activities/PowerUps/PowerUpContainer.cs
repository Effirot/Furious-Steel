using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using CharacterSystem.PowerUps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PowerUpContainer : NetworkBehaviour
{
    [field : SerializeField]
    public int Id { get; set; }

    public PowerUp powerUp => PowerUp.IdToPowerUpLink(Id);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, LayerMask.GetMask("Ground")))
            {
                SetPosition_ClientRpc(hit.point + Vector3.up * 1);
            }
            else
            {
                SetPosition_ClientRpc(transform.position);
            }
        }
    }

    [ClientRpc]
    private void SetPosition_ClientRpc(Vector3 position)
    {
        transform.position = position;
    }

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