
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

    public float Time { 
        get => time;
        set {
            value %= 24f;

            time = value;
        }
    }

    private Vector3 angle => new Vector3((time / 24f * 360f) - 180f, 0, 0);

    private void Awake()
    {
        Singleton = this;
    }
    private void Start()
    {
        transform.localEulerAngles = angle;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        transform.localEulerAngles = angle;
    }

    protected virtual void FixedUpdate()
    {
        Time += deltaTime * UnityEngine.Time.fixedDeltaTime;

        transform.localEulerAngles = new Vector3((time / 24f * 360f) - 180f, 0, 0);
    
        if (AutoEnableLaternsAfter12)
        {
            laternState = Time >= 8f ;
        }
    }

    protected virtual void OnTimeChangedHook(float Old, float New) { } 
}