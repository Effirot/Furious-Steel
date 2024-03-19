using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class InputListener : MonoBehaviour
{
    [SerializeField]
    private InputActionReference inputAction;

    private void Awake()
    {
        inputAction.action.Enable();

        inputAction.action.started += OnInputChanged_Event;
        inputAction.action.performed += OnInputChanged_Event;
        inputAction.action.canceled += OnInputChanged_Event;
    }

    private void OnDestroy()
    {
        inputAction.action.started -= OnInputChanged_Event;
        inputAction.action.performed -= OnInputChanged_Event;
        inputAction.action.canceled -= OnInputChanged_Event;
    }

    private void OnInputChanged_Event(CallbackContext collbackContex)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(collbackContex.ReadValueAsButton()); 
        }
    }
}