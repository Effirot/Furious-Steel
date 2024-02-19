using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class AutoAim : SyncedActivities<ISyncedActivitiesSource>
{
    private bool IsTargeted = false;
    private Vector3 aimPosition = Vector3.zero;
    

    protected override void OnStateChanged(bool IsPressed)
    {
        
    }

    private void LateUpdate()
    {
        
    }
}