
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

    protected override void Start()
    {
        base.Start();

        StartRegeneration(1, 0);
    }

    protected override void OnValueChangedHook(float Old, float New)
    {
        base.OnValueChangedHook(Old, New);

        StartRegeneration(Old, New);
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
            if (roundToInt)
            {
                Value += RegenerationPerSecond;

                yield return new WaitForSeconds(1);
            }
            else
            {
                Value += RegenerationPerSecond * Time.fixedDeltaTime;
            
                yield return new WaitForFixedUpdate();
            }
        }

        Value = MaxValue;
        regeneration = null;
    }
}