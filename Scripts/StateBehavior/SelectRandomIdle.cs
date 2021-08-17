using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class SelectRandomIdle : StateMachineBehaviour
{
    public int ClipCount = 4;
    public float timeremaining = 20;
    public int previousNumber = 0;
    public int randomNumber = 0;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (Time.time % timeremaining < Time.deltaTime)
        {
            previousNumber = randomNumber;
            randomNumber = Random.Range(1, ClipCount);
            if (randomNumber == previousNumber)
            {
                randomNumber -= 1;
            }

            if (randomNumber == -1 || randomNumber == 0)
            {
                randomNumber = ClipCount;
            }
            
            animator.SetFloat("randomidle", randomNumber);
        }
        else
        {
           // animator.SetInteger("randomidle",0);
        }
        
        //Debug.Log(Time.time % timeremaining < Time.deltaTime);
    }
}
