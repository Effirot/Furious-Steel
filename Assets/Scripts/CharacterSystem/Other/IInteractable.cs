
using System;
using CharacterSystem.DamageMath;

namespace CharacterSystem.Interactions 
{
    public interface IInteractable : 
        IGameObjectLink
    {
        public void OnSelect(IInteractor interactor);
        public void OnDeselect(IInteractor interactor);

        public void Interact(IInteractor interactor);    
    }

    public interface IInteractor : 
        IDamagable
    {
        
    }
}