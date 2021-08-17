using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetRotation : MonoBehaviour
{
    private float speed;
    
    private float Angle;
    private float direction;
    
    private float charAngle;

    private Vector2 playerInput;
    
    private float _turnVelocity;
    [SerializeField] 
    private float turnSmoothTime = 0.6f;

    private AnimatorStateInfo stateInfo;
    private AnimatorTransitionInfo transInfo;
    
    private Animator _animator;
    
    private int m_LocomotionId = 0;
    private int m_LocomotionPivotLId = 0;
    private int m_LocomotionPivotRId = 0;	
    private int m_LocomotionPivotLTransId = 0;	
    private int m_LocomotionPivotRTransId = 0;
    private int m_LocomotionFallId = 0;
    
    // Start is called before the first frame update
    
    public float LocomotionThreshold { get { return 0.2f; } }
    
    void Start()
    {
        _animator = GetComponent<Animator>();
        
        m_LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
        m_LocomotionPivotLId = Animator.StringToHash("Base Layer.LocomotionPivotL");
        m_LocomotionPivotRId = Animator.StringToHash("Base Layer.LocomotionPivotR");
        m_LocomotionPivotLTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotL");
        m_LocomotionPivotRTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotR");
        m_LocomotionFallId = Animator.StringToHash("Base Layer.LocomotionFall");
    }

    // Update is called once per frame
    void Update()
    {
        stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        transInfo = _animator.GetAnimatorTransitionInfo(0);
        
        speed = _animator.GetFloat("Speed");
        
        //_animator.SetFloat("Angle", charAngle); 
        
        charAngle = 0f;
        
        // Angles math
        
        Vector3 playerPos = transform.position;

        Vector3 playerDirection = transform.forward;
        
        Vector3 inputDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        
        // Get camera rotation
        Vector3 cameraLookPos = Camera.main.transform.forward;
        cameraLookPos.y = 0.0f;
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(cameraLookPos));

        // Convert joystick input in Worldspace coordinates

        Vector3 moveDirection = referentialShift * inputDirection;
        Vector3 axisSign = Vector3.Cross(moveDirection, playerDirection);

        Angle = Vector3.Angle(playerDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);

        Debug.DrawRay(new Vector3(playerPos.x, playerPos.y + 2f, playerPos.z), moveDirection, Color.green);
        Debug.DrawRay(new Vector3(playerPos.x, playerPos.y + 2f, playerPos.z), playerDirection, Color.magenta);
        Debug.DrawRay(new Vector3(playerPos.x, playerPos.y + 2f, playerPos.z), inputDirection, Color.blue);
        Debug.DrawRay(new Vector3(playerPos.x, playerPos.y + 2f, playerPos.z), axisSign, Color.red);

        
        if (!IsInPivot())
        { charAngle = Angle; }
        
        Angle /= 180;
        
        direction = Mathf.SmoothDamp(direction, Angle * 2, ref _turnVelocity, turnSmoothTime);
        
        //// Angles math

       

        _animator.SetFloat("Direction", direction);
        
        if (speed > LocomotionThreshold)	// Dead zone
        {
            if (!IsInPivot())
            { 
                _animator.SetFloat("Angle", charAngle);
            }

        }

        if (speed < LocomotionThreshold && Mathf.Abs(playerInput.x) < 0.05f)    // Dead zone
        {
            //_animator.SetFloat("Direction", 0f);
            _animator.SetFloat("Angle", 0f);
            Debug.Log("Dead Zone");
        }

    }

    private void FixedUpdate()
    {
        if (IsInLocomotion() && !IsInPivot() && ((direction >= 0 && playerInput.x >= 0) || (direction < 0 && playerInput.x < 0)))
        {
            Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, 120 * (playerInput.x < 0f ? -1f : 1f), 0f), Mathf.Abs(playerInput.x));
            Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
            transform.rotation *= deltaRotation;
        }
    }
    

    public bool IsInPivot()  // Is the character pivoting?
    {
        return stateInfo.fullPathHash == m_LocomotionPivotLId || 
               stateInfo.fullPathHash == m_LocomotionPivotRId || 
               transInfo.fullPathHash == m_LocomotionPivotLTransId || 
               transInfo.fullPathHash == m_LocomotionPivotRTransId;
    }
    
    public bool IsInLocomotion() // Is the character in Locomotion state?
    { return stateInfo.fullPathHash == m_LocomotionId; }
}
