using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlantRotation : StateMachineBehaviour
{
    public Transform player;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 rotation = animator.deltaRotation.eulerAngles;
        Debug.Log(rotation);
        rotation.y = 0f;

        player.Rotate(rotation);
    }
}

