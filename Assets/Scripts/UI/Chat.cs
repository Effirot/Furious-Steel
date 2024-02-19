using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cinemachine;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static RoomManager;

public class Chat : MonoBehaviour
{
    [SerializeField]
    private Text textField;

    [SerializeField]
    private TMP_InputField inputField;


    public void Write()
    {
        if (inputField.text == string.Empty)
            return;

        Write(inputField.text);

        inputField.text = "";
    }
    public void Write(string Text)
    {
        if (RoomManager.Singleton != null)
        {
            RoomManager.Singleton.WriteToChat(Text);
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        RoomManager.OnWriteToChat += OnWriteToChat;
    }

    private void OnWriteToChat(PublicClientData clientData, FixedString512Bytes text)
    {
        var color = clientData.spawnArguments.GetColor();
        
        textField.text += $"<color=#{color.ToHexString()}>{clientData.Name}</color> - {text}\n";
    }
}