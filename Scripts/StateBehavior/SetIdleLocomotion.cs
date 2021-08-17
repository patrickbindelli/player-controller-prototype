using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetIdleLocomotion : StateMachineBehaviour
{

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.GetFloat("Speed") < 0.5f)
        {
            animator.SetBool("isIdleling", true);
        }
        else
        {
            animator.SetBool("isIdleling", false);
        }
    }
}
