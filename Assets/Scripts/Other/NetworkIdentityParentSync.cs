

using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class NetworkIdentityParentSync : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnParentChanged))]
    public NetworkIdentity networkParent;
    
    public override void OnStartServer()
    {
        base.OnStartServer();

        UpdateParent();
    }

    private void OnParentChanged(NetworkIdentity Old, NetworkIdentity New)
    {
        if (isClient)
        {
            netIdentity.transform.SetParent(New?.transform ?? null, false);
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