using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public abstract class SyncedActivities<T> : SyncedActivities where T : ISyncedActivitiesSource
{
    public new T Invoker { 
        get 
        {
            if (base.Invoker == null)
            {
                base.Invoker = (ISyncedActivitiesSource) ResearchInvoker();
            }

            return (T) base.Invoker;
        } 
        private set => base.Invoker = value;
    }
    
    private T ResearchInvoker ()
    {
        var result = GetComponentInParent<T>();

        if (result == null)
        {
            Debug.LogWarning($"Unable to find activityes source {typeof(T).Name}");
        }

        return result;
    }

}
