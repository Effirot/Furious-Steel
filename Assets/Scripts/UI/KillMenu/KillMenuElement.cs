

using CharacterSystem.DamageMath;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class KillsMenuElement : MonoBehaviour
{

    [SerializeField]
    private TMP_Text KillerNameField;

    [SerializeField]
    private TMP_Text KilledNameField;

    [SerializeField]
    private float Lifetime = 1;
    
    [SerializeField]
    private Color ownerColor = Color.yellow;

    [SerializeField, Range(0, 1)]
    private float LerpForce = 0.1f;



    private Image image => GetComponent<Image>();

    public void Initialize(DamageDeliveryReport report)
    {
        KillerNameField.text = report.damage.sender.gameObject.name;
        KilledNameField.text = report.target.gameObject.name;

        if (report.damage.sender.IsOwner || (report.target.gameObject.TryGetComponent<NetworkObject>(out var net) && net.IsOwner))
        {
            image.color = ownerColor;
        }
    }

    private async void Start()
    {
        await UniTask.WaitForSeconds(Lifetime);
        
        Destroy(gameObject, 3);

        while (!this.IsUnityNull())
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(transform.localScale.x, 0, transform.localScale.z), LerpForce);

            await UniTask.WaitForEndOfFrame();
        }
    }
}