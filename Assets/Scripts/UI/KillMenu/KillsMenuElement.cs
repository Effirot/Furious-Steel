

using CharacterSystem.DamageMath;
using Cysharp.Threading.Tasks;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class KillsMenuElement : MonoBehaviour
{

    [SerializeField]
    private TMP_Text killerNameField;

    [SerializeField]
    private TMP_Text killedNameField;

    [SerializeField]
    private float lifetime = 1;
    
    [SerializeField]
    private Color ownerColor = Color.yellow;

    [SerializeField, Range(0, 1)]
    private float lerpForce = 0.1f;

    private Image image => GetComponent<Image>();

    public void Initialize(string KillerName, string KilledName)
    {
        killerNameField.SetText(KillerName);
        killedNameField.SetText(KilledName);

        // if ((report.damage.sender != null && report.damage.sender.isLocalPlayer) || 
        //     (report.target.gameObject.TryGetComponent<NetworkIdentity>(out var net) && net.isOwned))
        // {
        //     image.color = ownerColor;
        // }
    }

    private async void Start()
    {
        foreach (var fit in GetComponentsInChildren<ContentSizeFitter>())
        {
            fit.SetLayoutVertical();
            fit.SetLayoutHorizontal();
        }


        await UniTask.WaitForSeconds(lifetime);

        while (!this.IsUnityNull() && transform.localScale.y > 0.02f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(transform.localScale.x, 0, transform.localScale.z), lerpForce);

            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
        }

        if (!this.IsUnityNull())
        {
            transform.localScale = new Vector3(transform.localScale.x, 0, transform.localScale.z);

            Destroy(gameObject, 0.2f);
        }
    }
}