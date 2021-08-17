using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetRandomToZero : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetFloat("randomidle",0);
    }
}
