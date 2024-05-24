using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AnimatorOverrider : NetworkBehaviour
{
    [System.Serializable]
    private struct StringClipAnimatorOverrideLink
    {
        public string Name;
        
        public AnimationClip Clip;
    }
 
    [SerializeField]
    private List<StringClipAnimatorOverrideLink> overrides;

    private Animator lastAnimator;
    private AnimatorOverrideController localOverrider;

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        ClearAnimationOverrides();

        if (parentNetworkObject == null) 
            return;
        
        var animator = parentNetworkObject.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            if (animator.runtimeAnimatorController is AnimatorOverrideController)
            {
                localOverrider = animator.runtimeAnimatorController as AnimatorOverrideController;
                localOverrider.name += " (" + gameObject.name + ")";
            }
            else
            {
                localOverrider = new AnimatorOverrideController(animator.runtimeAnimatorController);
                localOverrider.name = "AnimationOverride (" + gameObject.name + ")";
            }

            
            List<KeyValuePair<AnimationClip, AnimationClip>> result = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var Value in overrides)
            {
                var clip = Array.Find(localOverrider.animationClips, clip => clip.name == Value.Name);
            
                if (clip != null)
                {
                    result.Add(new (clip, Value.Clip));
                }
            }
            localOverrider.ApplyOverrides(result);

            lastAnimator = animator;
            animator.runtimeAnimatorController = localOverrider;
        }
    }
    public override void OnNetworkDespawn()
    {
        ClearAnimationOverrides();

        base.OnNetworkDespawn();
    }

    private void ClearAnimationOverrides()
    {
        if (localOverrider != null)
        {
            lastAnimator.runtimeAnimatorController = localOverrider.runtimeAnimatorController;
            Destroy(localOverrider);
        }
    }
}
