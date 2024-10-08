using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using CharacterSystem.PowerUps;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.VFX;
using static UnityEngine.InputSystem.InputAction;

[DisallowMultipleComponent]
public class StealthObject : MonoBehaviour
{
    public List<StealthObject> Siblings = new();
    public List<CharacterStealthGraphicHider> characterSteathers = new();
}