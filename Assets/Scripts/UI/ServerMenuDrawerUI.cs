using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static RoomManager;

public class ServerMenuDrawerUI : MonoBehaviour
{
    [SerializeField]
    private GameObject prefab;

    private Dictionary<ConnectedPlayerData, ServerMenuDrawerPlayerData> clientDataFields = new();

    private void OnEnable()
    {
        ConnectedPlayerData.onConnectionDataChanged += OnPlayerListChanged_Event;

        RefreshAll();
    }
    private void OnDisable()
    {
        ConnectedPlayerData.onConnectionDataChanged -= OnPlayerListChanged_Event;

        Clear();
    }

    private void OnPlayerListChanged_Event(ConnectedPlayerData data)
    {
        UpdateInfo(clientDataFields[data], data);
    }

    private ServerMenuDrawerPlayerData Create(ConnectedPlayerData clientData)
    {
        var DataField = Instantiate(prefab, transform);
        DataField.SetActive(true);

        var menuField = DataField.GetComponent<ServerMenuDrawerPlayerData>();

        UpdateInfo(menuField, clientData);

        return menuField;
    }

    private void UpdateInfo(ServerMenuDrawerPlayerData playerField, ConnectedPlayerData clientData)
    {
        playerField.NameField.text = clientData.Name;
        
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
            Destroy(item.Value.gameObject);
        }

        clientDataFields.Clear();
    }

    private void RefreshAll()
    {
        Clear();

        foreach (var data in ConnectedPlayerData.All)
        {
            clientDataFields.Add(data, Create(data));
        }
    }
}