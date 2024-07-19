

using System.Collections;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CharacterSystem.Interactions
{
    public class PickUpInteractorActivity : SyncedActivitySource<IThrower>
    {
        [SerializeField]
        private Vector3 PickRayStart;
        [SerializeField]
        private Vector3 PickRayDirection;
        
        [Space]
        [SerializeField]
        private Vector3 PushForce;

        [Space]
        [SerializeField]
        private CharacterPermission pickTimePermissions = CharacterPermission.Default;

        [SerializeField]
        private CharacterPermission throwPermisssions = CharacterPermission.Default;

        [SerializeField, Range(0, 2)]
        private float throwDelay = 0.5f;
        
        [SerializeField]
        private CharacterPermission afterThrowPermisssions = CharacterPermission.Default;

        [SerializeField, Range(0, 2)]
        private float afterThrowDelay = 0.5f;

        public IInteractable AvailableInteractableObject { 
            get => availableInteractableObject; 
            set {
                if (!object.ReferenceEquals(availableInteractableObject, value))
                {
                    availableInteractableObject?.OnDeselect(Source);
                    availableInteractableObject = value;
                    availableInteractableObject?.OnSelect(Source);
                }
            }
        } 
        
        private IInteractable availableInteractableObject = null;
        private IThrowable pickedObject = null;

        private IInteractable GetInteractable() 
        {
            if (Physics.Raycast(
                transform.position + (transform.rotation * PickRayStart), 
                transform.rotation * PickRayDirection,
                out var hitInfo,
                PickRayDirection.magnitude) && hitInfo.collider.gameObject.TryGetComponent<IInteractable>(out var Interactable))
            {
                return Interactable;
            }
            else
            {
                return null;
            }
        }

        public override void Play()
        {
            if (!Source.activities.Any())
            {
                base.Play();
            }
        }

        public override IEnumerator Process()
        {   
            var interactable = GetInteractable();

            Source.pickPoint = transform;

            if (interactable.IsUnityNull())
                yield break;
            
            if (interactable is IThrowable)
            {
                Permissions = pickTimePermissions;
                
                pickedObject = interactable as IThrowable;
                
                pickedObject.Pick(Source);

                yield return new WaitWhile(() => IsPressed);
                yield return new WaitUntil(() => IsPressed);

                pickedObject.Interact(Source);

                yield return new WaitWhile(() => IsPressed);

                Permissions = throwPermisssions;
                yield return new WaitForSeconds(throwDelay);

                Drop(transform.rotation * (PushForce / 2));

                Permissions = afterThrowPermisssions;
                yield return new WaitForSeconds(afterThrowDelay);
            }
            else
            {
                interactable.Interact(Source);
            }
        }

        private void Drop(Vector3 direction)
        {
            if (!pickedObject.IsUnityNull())
            {
                pickedObject.Push(direction);
                Source.Push(-direction);

                pickedObject.Throw(Source);
                pickedObject = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawRay(transform.position + transform.rotation * PickRayStart, transform.rotation * PickRayDirection);
        }
        private void FixedUpdate()
        {
            if (isOwned)
            {
                AvailableInteractableObject = GetInteractable();
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            Drop(Vector3.up);
        }
    }
}