using CharacterSystem.Attacks;
using UnityEngine;

namespace CharacterSystem.Interactions 
{
    public interface IThrowable : 
        IInteractable, 
        IPhysicObject
    {
        public void Pick(IThrower thrower);
        public void Throw(IThrower thrower);
    }

    public interface IThrower : 
        ISyncedActivitiesSource,
        IInteractor,
        IPhysicObject,
        IDamageSource
    {
        public Transform pickPoint { get; set; }
    }
}