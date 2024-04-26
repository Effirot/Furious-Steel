using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static RoomManager;

public class ServerMenuDrawerUI : MonoBehaviour
{
    [SerializeField]
    private GameObject prefab;

    private List<ServerMenuDrawerPlayerData> clientDataFields = new List<ServerMenuDrawerPlayerData>();

    
    private void OnEnable()
    {
        RoomManager.Singleton.playersData.OnListChanged += OnPlayerListChanged_Event;

        RefreshAll();
    }
    private void OnDisable()
    {
        RoomManager.Singleton.playersData.OnListChanged -= OnPlayerListChanged_Event;

        Clear();
    }

    private void OnPlayerListChanged_Event(NetworkListEvent<PublicClientData> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<PublicClientData>.EventType.Add:
                var field = Create(changeEvent.Value);
                
                clientDataFields.Insert(changeEvent.Index, field);
                
                field.transform.SetSiblingIndex(changeEvent.Index);
            break;

            case NetworkListEvent<PublicClientData>.EventType.Insert : 
                goto case NetworkListEvent<PublicClientData>.EventType.Add;

            case NetworkListEvent<PublicClientData>.EventType.Remove:
                Destroy(clientDataFields[changeEvent.Index].gameObject);

                clientDataFields.RemoveAt(changeEvent.Index);
            break;

            case NetworkListEvent<PublicClientData>.EventType.RemoveAt:
                goto case NetworkListEvent<PublicClientData>.EventType.Remove;

            case NetworkListEvent<PublicClientData>.EventType.Value:
                UpdateInfo(clientDataFields[changeEvent.Index], changeEvent.Value);
            break;

            case NetworkListEvent<PublicClientData>.EventType.Clear:
                Clear();
            break;

            case NetworkListEvent<PublicClientData>.EventType.Full:
                RefreshAll();
            break;
        }
    }

    private ServerMenuDrawerPlayerData Create(PublicClientData clientData)
    {
        var DataField = Instantiate(prefab, transform);
        DataField.SetActive(true);

        var menuField = DataField.GetComponent<ServerMenuDrawerPlayerData>();

        UpdateInfo(menuField, clientData);

        return menuField;
    }

    private void UpdateInfo(ServerMenuDrawerPlayerData playerField, PublicClientData clientData)
    {
        playerField.NameField.text = clientData.Name.Value;
        
        if (playerField.KillstreakField != null)
        {
            playerField.KillstreakField.text = clientData.statistics.Points.ToString();
        }
        if (playerField.TotalKillstreakField != null)
        {
            playerField.TotalKillstreakField.text = clientData.statistics.KillStreakTotal.ToString();
        }
        if (playerField.AssistsField != null)
        {
            playerField.AssistsField.text = clientData.statistics.AssistsStreak.ToString();
        }
        if (playerField.TotalAssistsField != null)
        {
            playerField.TotalAssistsField.text = clientData.statistics.AssistsStreakTotal.ToString();
        }
        if (playerField.DeliveredDamage != null)
        {
            playerField.DeliveredDamage.text = clientData.statistics.DeliveredDamage.ToShortString();
        }
    }

    private void Clear()
    {
        foreach (var item in clientDataFields)
        {
            Destroy(item.gameObject);
        }

        clientDataFields.Clear();
    }

    private void RefreshAll()
    {
        Clear();

        foreach (var data in RoomManager.Singleton.playersData)
        {
            clientDataFields.Add(Create(data));
        }
    }
}