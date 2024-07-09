
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
[DisallowMultipleComponent]
public class NetworkIdentityParentSync : NetworkBehaviour
{
    [SyncVar(hook = nameof(ParentChanged))]
    public NetworkIdentity networkParent;
    
    public override void OnStartServer()
    {
        base.OnStartServer();

        UpdateParent();
    }

    private void ParentChanged(NetworkIdentity Old, NetworkIdentity New)
    {
        if (isClient)
        {
            netIdentity.transform.SetParent(New?.transform ?? null, false);
            
            gameObject.SendMessageUpwards("OnParentChanged", New);
        }
    }

    private async void UpdateParent()
    {
        if (isServer)
        {
            if (netIdentity.transform.parent == null)
            {
                networkParent = null;
                return;
            }

            NetworkIdentity newParent = netIdentity.transform.parent.GetComponent<NetworkIdentity>();
            
            if (newParent == null)
            {
                newParent = netIdentity.transform.parent.GetComponentInParent<NetworkIdentity>();
            }

            if (newParent != null)
            {
                await UniTask.WaitUntil(() => NetworkServer.spawned.ContainsKey(newParent.netId));

                networkParent = newParent;
            }
            else
            {
                networkParent = null;
            }
        }
    }
}