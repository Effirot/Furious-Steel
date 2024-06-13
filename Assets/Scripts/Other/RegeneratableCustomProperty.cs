
using System.Collections;
using UnityEngine;

public class RegeneratableCustomProperty : CustomProperty
{
    [Space]
    [SerializeField, Range(0, 100)]
    public float RegenerationPerSecond = 1f;
    
    [SerializeField, Range(0, 10)]
    public float BeforeRegenerationTimeout = 1f;
    
    private Coroutine regeneration;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        network_value.OnValueChanged += StartRegeneration;
        StartRegeneration(1, 0);
    }

    private void StartRegeneration(float Old, float New)
    {
        if (New - Old < 0 || regeneration == null)
        {
            if (regeneration != null)
            {
                StopCoroutine(regeneration);
            
                regeneration = null;
            }

            regeneration = StartCoroutine(Regeneration());
        }
    }

    private IEnumerator Regeneration()
    {
        yield return new WaitForSeconds(BeforeRegenerationTimeout);

        while (Value < MaxValue)
        {
            Value += RegenerationPerSecond * Time.fixedDeltaTime;
        
            yield return new WaitForFixedUpdate();
        }

        Value = MaxValue;
        regeneration = null;
    }
}