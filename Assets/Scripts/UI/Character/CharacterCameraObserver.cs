using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public interface IObservableObject
{
    public Transform ObservingPoint { get; }

    public bool IsOwner { get; }
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

            if (value == null || !(value.IsOwner))
            {
                StartCoroutine(ObserveRandomCharacter());
            }
        }
    }

    [SerializeField]
    private IObservableObject _observingCharacter = null;

    [SerializeField]
    private CinemachineVirtualCamera virtualCamera; 

    private void Awake()
    {
        Singleton = this;
        ObservingObject = null;
    
        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead += ResetObserver_Event;
    }
    private void OnDestroy()
    {
        Singleton = null;

        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead -= ResetObserver_Event;
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
            ObservingObject = array[Random.Range(0, PlayerNetworkCharacter.Players.Count - 1)];
        }
    }
}
