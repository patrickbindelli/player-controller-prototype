using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnRootMotion : StateMachineBehaviour
{
    
    public bool turnOffEnter = false;
    public bool turnOnEnter = false;
    
    public bool turnOffExit = false;
    public bool turnOnExit = false;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (turnOnExit)
        {
            animator.applyRootMotion = true;
        }

        if (turnOffExit)
        {
            animator.applyRootMotion = false;
        }
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (turnOnEnter)
        {
            animator.applyRootMotion = true;
        }

        if (turnOffEnter)
        {
            animator.applyRootMotion = false;
        }
    }
}
