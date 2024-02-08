using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class TouchscreenSwitcher : MonoBehaviour
{
    [SerializeField]
    private UnityEvent<bool> onTouchScreen = new();

    private void Awake()
    {
        onTouchScreen.Invoke(Array.Exists(InputSystem.devices.ToArray(), device => device.GetType() == typeof(Touchscreen)));
        
        InputSystem.onDeviceChange += DeviceChangedEvent;
    }

    private void DeviceChangedEvent(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
    {
        onTouchScreen.Invoke(Array.Exists(InputSystem.devices.ToArray(), device => device.GetType() == typeof(Touchscreen)));
    }
}
