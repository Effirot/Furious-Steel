using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AnimatorOverrider : NetworkBehaviour
{
    [SerializeField]
    private RuntimeAnimatorController animatorController;

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        if (parentNetworkObject != null)
        {
            var animator = parentNetworkObject.GetComponentInChildren<Animator>();

#error Override controller
            var a = new AnimatorOverrideController();
            
            if (animator != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }

        }
    }
}
