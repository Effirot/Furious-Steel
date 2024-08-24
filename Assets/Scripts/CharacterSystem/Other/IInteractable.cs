
using System;
using System.Collections;
using CharacterSystem.DamageMath;

namespace CharacterSystem.Interactions 
{
    public interface IInteractable : 
        IGameObjectLink
    {
        public bool IsInteractionAllowed(IInteractor interactor);

        public IEnumerator Interact(IInteractor interactor);    
    }

    public interface IInteractor : 
        IDamagable
    {
        
    }
}