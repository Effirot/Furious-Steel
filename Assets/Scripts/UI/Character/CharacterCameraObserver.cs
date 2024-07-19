using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Mirror;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public interface IObservableObject
{
    public Transform ObservingPoint { get; }
}

public class CharacterCameraObserver : MonoBehaviour
{
    public static CharacterCameraObserver Singleton { get; private set; }

    public IObservableObject ObservingObject
    {
        get => _observingCharacter;
        set
        {
            _observingCharacter = value;

            if (value != null)
            {
                virtualCamera.Follow = value.ObservingPoint;
            }

            StopAllCoroutines();

            if (value == null || !(value.ObservingPoint.TryGetComponent<NetworkIdentity>(out var identity) && identity.isLocalPlayer))
            {
                StartCoroutine(ObserveRandomCharacter());
            }
        }
    }

    [SerializeField]
    private IObservableObject _observingCharacter = null;

    [SerializeField]
    private CinemachineCamera virtualCamera; 

    [SerializeField]
    private InputActionReference scrollInput;

    private CinemachineFollowZoom zoom;
    private float zoomValue = 30f;

    private const float MinZoomValue = 10;
    private const float MaxZoomValue = 20;

    private void Awake()
    {
        Singleton = this;
        ObservingObject = null;
    
        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead += ResetObserver_Event;

        zoom = virtualCamera.GetComponent<CinemachineFollowZoom>();

        if (zoom != null && scrollInput != null)
        {
            scrollInput.action.Enable();
            scrollInput.action.performed += OnZoomAction;
            scrollInput.action.started += OnZoomAction;
            scrollInput.action.canceled += OnZoomAction;
        }
    }
    private void OnDestroy()
    {
        Singleton = null;

        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead -= ResetObserver_Event;

        if (zoom != null && scrollInput != null)
        {
            scrollInput.action.performed -= OnZoomAction;
            scrollInput.action.started -= OnZoomAction;
            scrollInput.action.canceled -= OnZoomAction;
        }
    }
    private void LateUpdate()
    {
        if (zoom != null)
        {
            zoom.Width = zoomValue;
        }
    }

    private void OnZoomAction(CallbackContext callback)
    {
        zoomValue = Mathf.Clamp(zoomValue - callback.ReadValue<float>(), MinZoomValue, MaxZoomValue);
    }


    private void ResetObserver_Event(PlayerNetworkCharacter character)
    {
        ObservingObject = null;
    }

    private IEnumerator ObserveRandomCharacter()
    {
        yield return new WaitForSecondsRealtime(5);

        var array = NetworkCharacter.NetworkCharacters.Where(character => character is IObservableObject).Select(character => character as IObservableObject).ToArray();

        if (PlayerNetworkCharacter.Players.Count == 0)
        {
            ObservingObject = null;
        }
        else
        {
            NextCharacter();
        }
    }

    private void NextCharacter()
    {
        var index = PlayerNetworkCharacter.Players.FindIndex(character => object.ReferenceEquals(character, PlayerNetworkCharacter.Players));
        
        if (index >= PlayerNetworkCharacter.Players.Count + 1)
        {
            index = 0;
        }
        else
        {
            index += 1;
        }

        if (index == -1)
        {
            ObservingObject = null;
        }
        else
        {
            ObservingObject = PlayerNetworkCharacter.Players[index];
        }
    }
    private void LastCharacter()
    {

    }


}
