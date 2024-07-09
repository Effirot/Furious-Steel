
using Unity.VisualScripting;
using UnityEngine;

public abstract class SyncedActivitySource<T> : SyncedActivitySource where T : ISyncedActivitiesSource
{
    public new T Source {
        get 
        {
            if (base.Source.IsUnityNull())
            {
                base.Source = null;
            }

            base.Source ??= GetComponent<T>();
            base.Source ??= GetComponentInParent<T>();

            if (base.Source.IsUnityNull())
            {
                Debug.LogWarning($"{typeof(T).Name} is not found\n{gameObject.name}");
            }

            return (T) base.Source;
        } 
        private set => base.Source = value;
    }
}
