using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class InputListener : MonoBehaviour
{
    [SerializeField]
    private InputActionReference inputAction;

    [SerializeField]
    private bool SwitchMode = false;

    private bool SwitchState = false;

    private void OnEnable()
    {
        inputAction.action.Enable();

        // inputAction.action.started += OnInputChanged_Event;
        inputAction.action.performed += OnInputChanged_Event;
        inputAction.action.canceled += OnInputChanged_Event;
    }
    private void OnDisable()
    {
        // inputAction.action.started -= OnInputChanged_Event;
        inputAction.action.performed -= OnInputChanged_Event;
        inputAction.action.canceled -= OnInputChanged_Event;
    }

    private void OnDestroy() => OnDisable();

    private void OnInputChanged_Event(CallbackContext collbackContex)
    {
        if (SwitchMode)
        {
            if (collbackContex.ReadValueAsButton())
            {
                SwitchState = !SwitchState;

                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(!child.gameObject.activeSelf);
                }   
            }
        }
        else
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(collbackContex.ReadValueAsButton());    
            }
        }
    }
}