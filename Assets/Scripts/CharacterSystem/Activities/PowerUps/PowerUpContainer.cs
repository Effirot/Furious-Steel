using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Mirror;
using UnityEngine;
using CharacterSystem.PowerUps;
using Cysharp.Threading.Tasks;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class PowerUpContainer : NetworkBehaviour
{
    [field : SerializeField]
    public int Id { get; set; }

    public PowerUp powerUp => PowerUp.IdToPowerUpLink(Id);

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isServer)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, LayerMask.GetMask("Ground")))
            {
                SetPosition_Command(hit.point + Vector3.up * 1);
            }
            else
            {
                SetPosition_Command(transform.position);
            }
        }
    }

    [Server, Command(requiresAuthority = false)]
    private void SetPosition_Command(Vector3 position)
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