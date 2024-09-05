

using System;
using UnityEngine;
using Mirror;
using CharacterSystem.Objects;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Drawing;
using CharacterSystem.DamageMath;
using CharacterSystem.Attacks;
using Unity.VisualScripting;
using CharacterSystem.Effects;

[DisallowMultipleComponent]
public class CharacterVoidPuddleManager : NetworkBehaviour
{
    private struct PuddleData : IEquatable<PuddleData>
    {
        public float size { get; }
        public Vector3 position { get; }
        public Vector3 normal { get; }

        public PuddleData (float size, Vector3 position, Vector3 normal)
        {
            this.size = size; 
            this.position = position; 
            this.normal = normal; 
        } 

        public bool Equals(PuddleData other)
        {
            return other.size == size && other.position == position && other.normal == normal;
        }
    }

    [SerializeField]
    private GameObject publePrefab;

    [SerializeField, Range(0, 25)]
    private int puddleLimit = 12;

    [SerializeField]
    private bool emmitPubles = true;

    [SerializeField]
    private float emmitDelay = 0.3f;

    [SerializeField]
    private float emmitSize = 8f;

    private NetworkCharacter networkCharacter;
    private readonly SyncList<PuddleData> puddleDatas = new(Array.Empty<PuddleData>());

    private List<GameObject> publeInstances = new();

    public void SetPuddle()
    {
        if (isServer)
        {
            if (Physics.Raycast(transform.position + Vector3.up + new Vector3(UnityEngine.Random.Range(-0.65f, 0.65f), 0, UnityEngine.Random.Range(-0.65f, 0.65f)), Vector3.down, out var hit, 5))
            {
                SetPuddle(hit.point, -hit.normal, emmitSize);   
            }
        }
    }
    public void SetPuddle(DamageDeliveryReport damageDeliveryReport)
    {
        if (isServer)
        {
            if (Physics.Raycast(damageDeliveryReport.target.transform.position + Vector3.up + new Vector3(UnityEngine.Random.Range(-0.65f, 0.65f), 0, UnityEngine.Random.Range(-0.65f, 0.65f)), Vector3.down, out var hit, 5))
            {
                SetPuddle(hit.point, -hit.normal, emmitSize);   
            }
        }
    }
    public void SetPuddle(Vector3 position, Vector3 normal, float size)
    {
        if (isServer)
        {
            if (puddleDatas.Count < puddleLimit)
            {
                puddleDatas.Add(new PuddleData(size, position, normal));
            }
        }
    }

    private void Start()
    {
        if (isServer && emmitPubles)
        {
            StartCoroutine(PubleEmission());
        }

        FindCharacter();
    }
    private void OnDestroy()
    {
        Clear();
    }
    private void Awake()
    {
        networkCharacter = GetComponentInParent<NetworkCharacter>();

        if (networkCharacter.IsUnityNull())
        {
            networkCharacter = GetComponent<NetworkCharacter>();
        }
        puddleDatas.OnChange += OnPubleDataListChanged_Event;
    }

#if !UNITY_SERVER || UNITY_EDITOR
    private void LateUpdate()
    {
        for (int i = 0; i < Mathf.Min(puddleDatas.Count, publeInstances.Count); i++)
        {
            var publeData = puddleDatas[i];
            var puddle = publeInstances[i];

            var projector = puddle.GetComponent<DecalProjector>();
            projector.size = Vector3.Lerp(projector.size, Vector3.one * publeData.size, 0.03f);
        }
    }
#endif
    private void FixedUpdate()
    {
        if (isServer)
        {
            for (int i = 0; i < puddleDatas.Count; i++)
            {
                var data = puddleDatas[i];
                puddleDatas[i] = new PuddleData(data.size - Time.fixedDeltaTime * 2, data.position, data.normal);

                if (data.size <= 0)
                {
                    puddleDatas.RemoveAt(i);
                    
                    i--;
                }

                foreach (var hit in Physics.OverlapSphere(data.position, data.size / 2, LayerMask.GetMask("Character")))
                {
                    Damage.Deliver(
                        hit.gameObject, 
                        new Damage(
                            Time.fixedDeltaTime, 
                            networkCharacter is IAttackSource ? networkCharacter as IAttackSource : null, 
                            0, 
                            Vector3.zero, 
                            Damage.Type.Effect, 
                            new SlownessEffect(1, 3)));
                }
            }
        }
    }

    private void OnPubleDataListChanged_Event(SyncList<PuddleData>.Operation operation, int index, PuddleData puddleData)
    {
        switch (operation)
        {
            case SyncList<PuddleData>.Operation.OP_ADD: 
                CreateObject(puddleData);
                break;

            case SyncList<PuddleData>.Operation.OP_SET: 
                SetPubleData(publeInstances[index], puddleData);
                break;

            case SyncList<PuddleData>.Operation.OP_INSERT: 
                goto case SyncList<PuddleData>.Operation.OP_ADD;
                
            case SyncList<PuddleData>.Operation.OP_REMOVEAT: 
                Remove(publeInstances[index]);
                break;
            
            case SyncList<PuddleData>.Operation.OP_CLEAR: 
                Clear();
                break;      
        }
    }

    private void CreateObject(PuddleData publeData)
    {
        var publeObject = Instantiate(publePrefab);
        publeObject.SetActive(true);
        publeInstances.Add(publeObject);

        SetPubleData(publeObject, publeData);
    }
    private void SetPubleData(GameObject puddle, PuddleData publeData)
    {       
        puddle.transform.position = publeData.position;
        puddle.transform.rotation = Quaternion.LookRotation(publeData.normal);
    }
    private void Remove(GameObject puddle)
    {
        publeInstances.Remove(puddle);
        Destroy(puddle);
    }
    private void Refresh()
    {
        Clear();

        foreach (var data in puddleDatas)
        {
            CreateObject(data);
        }
    }
    private void Clear()
    {
        foreach (var puddle in publeInstances)
        {
            Destroy(puddle);
        }
        
        publeInstances.Clear();
    }

    private IEnumerator PubleEmission()
    {
        while (true)
        {
            yield return new WaitForSeconds(emmitDelay);

            SetPuddle();
        }
    }

    private void FindCharacter()
    {
        networkCharacter = GetComponentInParent<NetworkCharacter>();

        if (networkCharacter.IsUnityNull())
        {
            networkCharacter = GetComponent<NetworkCharacter>();
        }
    }
}