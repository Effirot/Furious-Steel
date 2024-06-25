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

    [SyncVar(hook = nameof(OnPositionChanged))]
    private Vector3 serverPosition;


    public override void OnStartServer()
    {
        base.OnStartServer();

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, LayerMask.GetMask("Ground")))
        {
            transform.position = serverPosition = hit.point + Vector3.up;
        }
        else
        {
            serverPosition = transform.position;
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        transform.position = serverPosition;
    }

    private void OnPositionChanged(Vector3 Old, Vector3 New)
    {
        if (!isServer && isClient)
        {
            transform.position = New;
        }
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