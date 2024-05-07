

using System;
using UnityEngine;
using Unity.Netcode;
using CharacterSystem.Objects;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Drawing;
using CharacterSystem.DamageMath;
using CharacterSystem.Attacks;
using Unity.VisualScripting;

[DisallowMultipleComponent]
public class CharacterVoidPuddleManager : NetworkBehaviour
{
    private struct PuddleData : 
        INetworkSerializable,
        IEquatable<PuddleData>
    {
        public float size;
        public Vector3 position;
        public Vector3 normal;

        public bool Equals(PuddleData other)
        {
            return other.size == size && other.position == position && other.normal == normal;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref size);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref normal);
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
    private NetworkList<PuddleData> puddleDatas;

    private List<GameObject> publeInstances = new();

    public void SetPuddle()
    {
        if (IsServer)
        {
            if (Physics.Raycast(transform.position + Vector3.up + new Vector3(UnityEngine.Random.Range(-0.65f, 0.65f), 0, UnityEngine.Random.Range(-0.65f, 0.65f)), Vector3.down, out var hit, 5))
            {
                SetPuddle(hit.point, -hit.normal, emmitSize);   
            }
        }
    }
    public void SetPuddle(DamageDeliveryReport damageDeliveryReport)
    {
        if (IsServer)
        {
            if (Physics.Raycast(damageDeliveryReport.target.transform.position + Vector3.up + new Vector3(UnityEngine.Random.Range(-0.65f, 0.65f), 0, UnityEngine.Random.Range(-0.65f, 0.65f)), Vector3.down, out var hit, 5))
            {
                SetPuddle(hit.point, -hit.normal, emmitSize);   
            }
        }
    }
    public void SetPuddle(Vector3 position, Vector3 normal, float size)
    {
        if (IsServer)
        {
            if (puddleDatas.Count < puddleLimit)
            {
                puddleDatas.Add(new PuddleData()
                {
                    position = position,
                    normal = normal,
                    size = size,
                });
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && emmitPubles)
        {
            StartCoroutine(PubleEmission());
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        Clear();
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);

        if (!parentNetworkObject.IsUnityNull())
        {
            FindCharacter();
        }
    }

    private void Awake()
    {
        networkCharacter = GetComponentInParent<NetworkCharacter>();

        if (networkCharacter.IsUnityNull())
        {
            networkCharacter = GetComponent<NetworkCharacter>();
        }

        puddleDatas = new(Array.Empty<PuddleData>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        puddleDatas.OnListChanged += OnPubleDataListChanged_Event;
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
        if (IsServer)
        {
            for (int i = 0; i < puddleDatas.Count; i++)
            {

                var data = puddleDatas[i];
                data.size -= Time.fixedDeltaTime * 2;
                puddleDatas[i] = data;

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
                            networkCharacter is IDamageSource ? networkCharacter as IDamageSource : null, 
                            0, 
                            Vector3.zero, 
                            Damage.Type.Effect, 
                            new SlownessEffect(1, 3)));
                }
            }
        }
    }

    private void OnPubleDataListChanged_Event(NetworkListEvent<PuddleData> networkListEvent)
    {
        switch (networkListEvent.Type)
        {
            case NetworkListEvent<PuddleData>.EventType.Add:
                CreateObject(networkListEvent.Value);
                break;

            case NetworkListEvent<PuddleData>.EventType.Insert: 
                goto case NetworkListEvent<PuddleData>.EventType.Add;

            case NetworkListEvent<PuddleData>.EventType.Remove:
                Remove(publeInstances[networkListEvent.Index]);
                break;

            case NetworkListEvent<PuddleData>.EventType.RemoveAt:
                goto case NetworkListEvent<PuddleData>.EventType.Remove;

            case NetworkListEvent<PuddleData>.EventType.Value:
                SetPubleData(publeInstances[networkListEvent.Index], networkListEvent.Value);
                break;

            case NetworkListEvent<PuddleData>.EventType.Clear:
                Clear();
                break;

            case NetworkListEvent<PuddleData>.EventType.Full:
                Refresh();
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