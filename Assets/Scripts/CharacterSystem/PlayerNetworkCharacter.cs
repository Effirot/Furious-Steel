using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class PlayerNetworkCharacter : NetworkCharacter
{
    [SerializeField]
    private InputActionReference moveInput;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            var action = moveInput.action;
            action.Enable();

            action.performed += OnMove;
            action.canceled += OnMove;

            LinkToMainCamera();
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    private void LinkToMainCamera()
    {
        if (Camera.main.TryGetComponent<CinemachineVirtualCamera>(out var virtualCamera))
        {
            virtualCamera.Follow = transform;
        }
    }

    private void OnMove(CallbackContext input)
    {
        SetMovementVector(input.ReadValue<Vector2>());
    }
}
