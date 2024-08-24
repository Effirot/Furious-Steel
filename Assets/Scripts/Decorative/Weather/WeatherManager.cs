
using Mirror;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class WeatherManager : NetworkBehaviour
{
    public static WeatherManager Singleton { get; private set; }

    [SerializeField]
    private bool AutoEnableLaternsAfter12 = false;

    [SerializeField]
    private bool SyncWithRealTime = false;

    [SerializeField, SyncVar]
    public bool laternState = false;

    [SerializeField, Range(0, 24), SyncVar(hook = nameof(OnTimeChangedHook))]
    public float time;

    [SerializeField, Range(0, 10)]
    private float deltaTime;

    [SerializeField]
    private Gradient lightGradient;

    [SerializeField]
    private new Light light;


    public float Time { 
        get { 
            if (SyncWithRealTime)
            {
                var currentTime = System.DateTime.Now;
                return time = currentTime.Hour + (currentTime.Minute / 600f) + (currentTime.Second / 10000f);
            }
            else
            {
                return time;
            }
        }
        set {
            value %= 24f;

            time = value;
        }
    }

    private void Awake()
    {
        Singleton = this;
    }


    protected override void OnValidate()
    {
        base.OnValidate();

        if (light != null)
        {
            light.color = lightGradient.Evaluate(Time / 24f);
        }
    }

    protected virtual void FixedUpdate()
    {
        Time += deltaTime * UnityEngine.Time.fixedDeltaTime;
    
        if (AutoEnableLaternsAfter12)
        {
            laternState = Time >= 8f ;
        }

        UpdateLight();
    }

    private void UpdateLight()
    {
        if (light != null)
        {
            light.color = Color.Lerp(light.color, lightGradient.Evaluate(Time / 24f), 18f * UnityEngine.Time.fixedDeltaTime);
        }
    }

    protected virtual void OnTimeChangedHook(float Old, float New) { } 
}