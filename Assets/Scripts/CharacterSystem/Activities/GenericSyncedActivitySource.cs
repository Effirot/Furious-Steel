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

public abstract class SyncedActivitySource<T> : SyncedActivitySource where T : ISyncedActivitiesSource
{
    public new T Source { 
        get 
        {
            if (base.Source == null)
            {
                base.Source = (ISyncedActivitiesSource) ResearchInvoker();
            }

            return (T) base.Source;
        } 
        private set => base.Source = value;
    }
    
    private T ResearchInvoker ()
    {
        T result = GetComponent<T>();

        if (result == null)
        {
            result = GetComponentInParent<T>();
        }

        if (result == null)
        {
            Debug.LogWarning($"Unable to find activityes source {typeof(T).Name}");
        }

        return result;
    }

}
