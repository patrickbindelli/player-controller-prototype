using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    private Rigidbody[] _rigidbodies;
    private Animator _animator;
    private CharacterController _character;
    void Start()
    {
        _rigidbodies = GetComponentsInChildren<Rigidbody>();
        _animator = GetComponent<Animator>();
        _character = GetComponent<CharacterController>();
        ActivateRagdoll();
    }


   public void DeactivateRagdoll()
    {
        foreach (var rigidbody in _rigidbodies)
        {
            rigidbody.isKinematic = true;
        }
        _animator.enabled = true;
        _character.enabled = true;
    }

   public void ActivateRagdoll()
   {
       foreach (var rigidbody in _rigidbodies)
       {
           rigidbody.isKinematic = false;
       }
       _animator.enabled = false;
       _character.enabled = false;
   }
}
