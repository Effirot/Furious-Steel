using TMPro;
using Unity.Collections;
using UnityEngine;
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
        RoomManager.OnWriteToChat += OnWriteToChat;
    }

    private void OnWriteToChat(PublicClientData clientData, FixedString512Bytes text)
    {        
        textField.text += $"{clientData.Name} - {text}\n";
    }
}