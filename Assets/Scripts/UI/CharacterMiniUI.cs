using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using TMPro;
using UnityEngine;

public class CharacterMiniUI : MonoBehaviour
{
    #error FIX IT PEACE OF SHIT    

    [SerializeField]
    private NetworkCharacter networkCharacter;
    
    [SerializeField]
    private TMP_Text NicknameField;

    
    private void Awake()
    {
        
    }

    private void LateUpdate()
    {
        transform.rotation = Camera.main.transform.rotation;
    }


}
