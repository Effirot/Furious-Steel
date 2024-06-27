
using UnityEngine;

public abstract class SyncedActivitySource<T> : SyncedActivitySource where T : ISyncedActivitiesSource
{
    public new T Source {
        get 
        {
            if (base.Source == null)
            {
                base.Source = ResearchInvoker();

                if (base.Source != null)
                {
                    OnFindSource((T) base.Source);
                }
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

            if (result == null)
            {
                Debug.LogWarning($"Unable to find activityes source {typeof(T).Name}");
            }
            else
            {   
                OnFindSource(result);
            }
        }

        return result;
    }

    public virtual void OnFindSource(T source) { }
}
