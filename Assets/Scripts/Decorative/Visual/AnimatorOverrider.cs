using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AnimatorOverrider : MonoBehaviour
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


#warning detecting parent changing
    // public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    // {
    //     ClearAnimationOverrides();

    //     if (parentNetworkObject == null) 
    //         return;
        
    //     UpdateParent(parentNetworkObject.gameObject);
    // }

    private void Start()
    {
        UpdateParent(transform.parent?.gameObject);
    }
    private void OnDestroy()
    {
        ClearAnimationOverrides();
    }

    private void UpdateParent(GameObject target)
    {
        if (target == null)
            return;

        var animator = target.GetComponentInChildren<Animator>();

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

    private void ClearAnimationOverrides()
    {
        if (localOverrider != null)
        {
            lastAnimator.runtimeAnimatorController = localOverrider.runtimeAnimatorController;
            Destroy(localOverrider);
        }
    }

    
}
