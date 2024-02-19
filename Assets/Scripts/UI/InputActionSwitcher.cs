



using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionSwitcher : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset actionScheme;

    [SerializeField]
    private int actionMapIndex = 0;

    private void OnEnable()
    {
        foreach (var item in actionScheme.actionMaps[actionMapIndex])
        {
            item.Enable();
        }
    }   

    private void OnDisable()
    {
        foreach (var item in actionScheme.actionMaps[actionMapIndex])
        {
            item.Disable();
        }
    }
}