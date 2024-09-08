
using System.Collections;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using JetBrains.Annotations;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace CharacterSystem.Interactions
{
    public interface IThrower : 
        ISyncedActivitiesSource,
        IInteractor,
        IPhysicObject,
        IAttackSource
    {

    }

    public class PickUpInteractorActivity : SyncedActivitySource<IThrower>
    {
        private static Material OutlineMaterial;

        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad()
        {
            OutlineMaterial = new Material(Shader.Find("Effirot/Outline"));
            OutlineMaterial.SetColor("_OutlineColor", Color.green); 
            OutlineMaterial.SetFloat("_OutlineWidth", 30); 
            OutlineMaterial.renderQueue = 3000;
        }

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
        
        [SerializeField]
        private CharacterPermission afterThrowPermisssions = CharacterPermission.Default;

        [SerializeField, Range(0, 2)]
        private float throwDelay = 0.5f;

        [SerializeField, Range(0, 2)]
        private float afterThrowDelay = 0.5f;
        
        [SerializeField]
        private bool DropPropOnHit = false;

        public IThrowable pickedObject = null;

        public IInteractable AvailableInteractableObject { 
            get => availableInteractableObject; 
            set {
                if (!object.ReferenceEquals(availableInteractableObject, value))
                {
                    if (isOwned && isClient && !availableInteractableObject.IsUnityNull())
                    {
                        foreach (var renderer in availableInteractableObject.gameObject.GetComponentsInChildren<Renderer>())
                        {
                            if (renderer is MeshRenderer)
                            {
                                var meshRenderer = renderer as MeshRenderer;

                                meshRenderer.sharedMaterials = meshRenderer.sharedMaterials.Where(material => material != OutlineMaterial).ToArray();
                            }

                            if (renderer is SkinnedMeshRenderer)
                            {
                                var skinnedMeshRenderer = renderer as SkinnedMeshRenderer;

                                skinnedMeshRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials.Where(material => material != OutlineMaterial).ToArray();
                            }
                        }
                    }

                    availableInteractableObject = value;

                    if (isOwned && isClient && !availableInteractableObject.IsUnityNull())
                    {                        
                        foreach (var renderer in availableInteractableObject.gameObject.GetComponentsInChildren<Renderer>())
                        {
                            if (renderer is MeshRenderer)
                            {
                                var meshRenderer = renderer as MeshRenderer;

                                meshRenderer.sharedMaterials = meshRenderer.sharedMaterials.Prepend(OutlineMaterial).ToArray();
                            }

                            if (renderer is SkinnedMeshRenderer)
                            {
                                var skinnedMeshRenderer = renderer as SkinnedMeshRenderer;

                                skinnedMeshRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials.Prepend(OutlineMaterial).ToArray();
                            }
                        }
                    }
                }
            }
        } 

        private IInteractable availableInteractableObject = null;
        

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
            while (Source.activities.Any())
            {
                Source.activities.First().Stop();
            }

            AvailableInteractableObject = GetInteractable();

            if (!pickedObject.IsUnityNull())
            {
                Stop();
            }

            base.Play();
        }

        public override IEnumerator Process()
        {
            var waitForFixedUpdate = new WaitForFixedUpdate();

            if (!pickedObject.IsUnityNull())
            {
                yield return pickedObject.Interact(Source);

                yield return new WaitWhile(() => IsPressed);

                var relativeVelocity = transform.InverseTransformDirection(Source.velocity);
                relativeVelocity.z = Mathf.Min(PushForce.z, relativeVelocity.z);

                Permissions = throwPermisssions;
                yield return new WaitForSeconds(throwDelay);

                Drop(transform.rotation * (PushForce + relativeVelocity));

                Permissions = afterThrowPermisssions;
                yield return new WaitForSeconds(afterThrowDelay);

                yield break;
            }

            if (AvailableInteractableObject is IThrowable)
            {
                var preparePickedObject = AvailableInteractableObject as IThrowable;
                
                if (preparePickedObject.IsInteractionAllowed(Source) && isServer)
                {
                    pickedObject = preparePickedObject;

                    Pick(preparePickedObject);
                }
            }

            if (!pickedObject.IsUnityNull())
            {
                

                while (true) 
                { 
                    Permissions = pickTimePermissions;    
                    
                    yield return waitForFixedUpdate; 
                }
            }
        }

        private void Drop(Vector3 direction)
        {
            if (!pickedObject.IsUnityNull())
            {
                pickedObject.Push(direction);
                Source.Push(-direction);
                
                if (isServer)
                {
                    SetPickedObject(0);
                }

                pickedObject.Throw(Source);
                pickedObject = null;
            }
        }
        private void Pick(IThrowable throwable)
        {
            AvailableInteractableObject = null;

            pickedObject = throwable;
            Permissions = pickTimePermissions;

            pickedObject.Pick(Source);
        }

        private void OnHitReaction_Event(Damage damage)
        {
            if (DropPropOnHit && damage.type is not Damage.Type.Effect and not Damage.Type.Magical)
            {
                Drop(Vector3.up / 2);
            }
        }

        [ClientRpc]
        private void SetPickedObject(uint ID)
        {
            if (!isServer)
                return;

            if (ID == 0 || ID >= NetworkClient.spawned.Count)
            {
                pickedObject = null;

                return;
            }

            var identity = NetworkClient.spawned[ID];

            Pick(identity.GetComponent<IThrowable>());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawRay(transform.position + transform.rotation * PickRayStart, transform.rotation * PickRayDirection);
        }
        private void FixedUpdate()
        {
            if (pickedObject.IsUnityNull())
            {
                if (isOwned)
                {
                    AvailableInteractableObject = GetInteractable();
                }            
            }
        }
        private void LateUpdate()
        {
            if (!pickedObject.IsUnityNull())
            {
                Source.permissions = pickTimePermissions;

                pickedObject.transform.position = transform.position;
                pickedObject.transform.rotation = transform.rotation;

                pickedObject.velocity = Vector3.zero;

                return;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Drop(Vector3.up);
            AvailableInteractableObject = null;
        }
    }
}