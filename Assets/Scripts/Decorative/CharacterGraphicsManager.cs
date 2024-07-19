using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class CharacterGraphicsManager : MonoBehaviour
{

    [SerializeField]
    private NetworkCharacter networkCharacter;

    [SerializeField]
    private AudioSource OnHitSound;
    [SerializeField]
    private List<VisualEffect> OnHitEffect;
    [SerializeField]
    private UnityEvent<Damage> OnHit = new();

    [SerializeField]
    private AudioSource OnHealSound;
    [SerializeField]
    private List<VisualEffect> OnHealEffect;
    [SerializeField]
    private UnityEvent<Damage> OnHeal = new();

    [SerializeField]
    private UnityEvent<bool> OnStunned = new ();
    [SerializeField]
    private UnityEvent OnJump = new ();
    [SerializeField]
    private UnityEvent<bool> OnGrounded = new ();
    [SerializeField]
    private UnityEvent<DamageDeliveryReport, DamageDeliveryReport> OnWallHit = new ();


    private void Start()
    {
        networkCharacter.onDamageRecieved += OnDamageRecieved_Event;

        networkCharacter.onStunStateChanged += OnStunned.Invoke;
        networkCharacter.isGroundedEvent += OnGrounded.Invoke;
        networkCharacter.onJumpEvent += OnJump.Invoke;
        networkCharacter.onWallHit += OnWallHit.Invoke;
    }
    private void OnDestroy()
    {

    }

    private void OnDamageRecieved_Event(Damage damage)
    {
        if (damage.value >= 0)
        {
            OnHit.Invoke(damage);
            OnHitSound.Play();

            foreach (var effect in OnHitEffect)
            {
                effect.SetVector3("Direction", damage.pushDirection);
                effect.Play();
            }
        }
        else
        {
            OnHeal.Invoke(damage);
            OnHealSound.Play();

            foreach (var effect in OnHealEffect)
            {
                effect.SetVector3("Direction", damage.pushDirection);
                effect.Play();
            }
        }
    }
}
