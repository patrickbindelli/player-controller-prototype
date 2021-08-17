using System;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;



public class PlayerControllerLogic : MonoBehaviour
{

    #region Variables

    public LayerMask layerMask;
    
    //PlayerComponents - selecionados automaticamente
    
    [Title("Player Components", "(Auto-Selected)")]
    [ShowInInspector]
    private Animator _animator;
    [ShowInInspector]
    private CharacterController _controller;
    [ShowInInspector]
    private CapsuleCollider _capsuleCollider;
    [ShowInInspector] 
    private Transform _transform;
    
    [Title("Scene Components", "(Pick these from your scene)")]
    [Required]
    [SerializeField]
    private GameObject playerCamera;
    
    //Movement Variables
    [Title("Movement Variables")]
    [SerializeField] 
    private float speedSmoothTime = 0.3f;
    [SerializeField] 
    private float turnSmoothTime = 0.6f;
    [SerializeField] 
    [Range(0f, 2f)] 
    private float runSpeed;
    [SerializeField] 
    private float rotationDegreePerSecond = 120f;
    
    //Jump Variables
    [Title("Jump Variables")]
    [SerializeField]
    private float gravity = 0f;
    [SerializeField]
    private float jumpHeight = 0f;
    [SerializeField] 
    private float jumpDamp = 0f;
    [SerializeField] 
    private float stepDown;
    [SerializeField] 
    private float airControl;
    
    [Title("IK")]
    [SerializeField]
    private float distanceToGround;
    
    [Title("For Debugging Purposes"), ReadOnly]
    [ShowInInspector]
    private Vector2 playerInput;
    [ShowInInspector, ReadOnly]
    private float speed = 0.0f;
    [ShowInInspector, ReadOnly]
    private float direction = 0.0f;
    [ShowInInspector, ReadOnly]
    private bool isInAir;

    //Private-Only
    private Vector3 playerPos;
    private float charAngle = 0f;
    private float charSpeed = 0f;
    private Vector3 cameraMove;
    private Vector3 velocity;
    private AnimatorStateInfo stateInfo;
    private AnimatorTransitionInfo transInfo;
    private float _turnVelocity;
    private float _speedVelocity;
    private float _currentSpeed; 
    
    // Hashes for performance
    private int m_LocomotionId = 0;
    private int m_LocomotionPivotLId = 0;
    private int m_LocomotionPivotRId = 0;	
    private int m_LocomotionPivotLTransId = 0;	
    private int m_LocomotionPivotRTransId = 0;
    private int m_IdleId = 0;
    private int m_LocomotionFallId = 0;

    private int m_Direction = 0;
    private int m_Speed = 0;
    private int m_Angle = 0;
    private int m_Jump = 0;
    private int m_Run = 0;
    private int m_OnGround = 0;
    
    private float moveTime = 0.0f;


    #endregion Variables

    #region Properties

    public float LocomotionThreshold { get { return 0.2f; } }

    #endregion Properties

    #region Unity Events

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _capsuleCollider = GetComponent<CapsuleCollider>();
        _controller = GetComponent<CharacterController>();
        _transform = GetComponent<Transform>();
        
        _animator = GetComponent<Animator>();
        if (_animator.layerCount >= 2) { _animator.SetLayerWeight(1,1); }
        
        // Hash all animation names for performance
        m_LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
        m_LocomotionPivotLId = Animator.StringToHash("Base Layer.LocomotionPivotL");
        m_LocomotionPivotRId = Animator.StringToHash("Base Layer.LocomotionPivotR");
        m_LocomotionPivotLTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotL");
        m_LocomotionPivotRTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotR");
        m_LocomotionFallId = Animator.StringToHash("Base Layer.LocomotionFall");
        
        m_IdleId = Animator.StringToHash("isIdleling");

        m_Direction = Animator.StringToHash("Direction");
        m_Speed = Animator.StringToHash("Speed");
        m_Angle = Animator.StringToHash("Angle");
        m_Jump = Animator.StringToHash("Jump");
        m_Run = Animator.StringToHash("Run");
        m_OnGround = Animator.StringToHash("OnGround");
    }
    private void FixedUpdate()
    {
        if (isInAir)
        {
            UpdateInAir();
        }else
        {
            UpdateOnGround();
        }
        
        if (IsInLocomotion() && !IsInPivot() && ((direction >= 0 && playerInput.x >= 0) || (direction < 0 && playerInput.x < 0)))
        {
            Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, rotationDegreePerSecond * (playerInput.x < 0f ? -1f : 1f), 0f), Mathf.Abs(playerInput.x));
            Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
            _transform.rotation *= deltaRotation;
        }
    }
    private void Update()
    {
        _animator.SetBool(m_OnGround, _controller.isGrounded);
        _animator.SetFloat("velocityY", velocity.y);

        RaycastHit hit;
        Ray ray = new Ray(transform.position, -transform.up);
        Debug.DrawRay(transform.position,-transform.up, Color.red);
        Physics.Raycast(ray, out hit);
        _animator.SetFloat("height", hit.distance);

        stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        transInfo = _animator.GetAnimatorTransitionInfo(0);

        charAngle = 0f;

        playerPos = transform.position;
        

        playerInput.x = Input.GetAxis("Horizontal"); 
        playerInput.y = Input.GetAxis("Vertical"); 
        
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        InputToWorldPos(ref charSpeed, ref cameraMove);
        moveTime += Time.deltaTime * charSpeed;
        
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
            {
                speed = Mathf.Lerp(speed, runSpeed, 5 * Time.deltaTime);
            } 
        }
        else
        {
            speed = charSpeed;
        }

        
        _animator.SetFloat(m_Speed, speed);
        _animator.SetFloat(m_Direction, direction);
        
        if (speed > LocomotionThreshold)	// Dead zone
        {
            if (!IsInPivot())
            { 
                _animator.SetFloat(m_Angle, charAngle);
            }
        }
		   
        if (speed < LocomotionThreshold && Mathf.Abs(playerInput.x) < 0.05f)    // Dead zone
        {
            _animator.SetFloat(m_Direction, 0f);
            _animator.SetFloat(m_Angle, 0f);
        }

        if (Input.GetButtonDown("Jump"))
        {
            _animator.SetBool(m_Jump, true);
            Jump();
        }
    }

    #endregion Unity Events

    #region Methods

    private void UpdateOnGround()
    {
        Vector3 stepDownAmount = Vector3.down * stepDown;
        _controller.Move(stepDownAmount);
        _animator.SetBool(m_Jump, false);

        if (!_controller.isGrounded)
        {
            SetInAir(0);
        }
    }

    private void UpdateInAir()
    {
        velocity.y -= gravity * Time.fixedDeltaTime;
        Vector3 displacement = velocity * Time.fixedDeltaTime;
        displacement += CalculateAirControl();
        _controller.Move(displacement);
        isInAir = !_controller.isGrounded;
    }
    
    private void Jump()
    {
        if (!isInAir)
        {
            float velocityY = Mathf.Sqrt(2 * gravity * jumpHeight);
            SetInAir(velocityY);
        }
    }

    private Vector3 CalculateAirControl()
    {
        return ((transform.forward * playerInput.y) + (transform.right * playerInput.x)) * (airControl / 100f);
    }
    private void SetInAir(float velocityY)
    {
        isInAir = true;
        velocity = _animator.velocity * jumpDamp;
        velocity.y = velocityY;
    }
    public bool IsInPivot()  // Is the character pivoting?
    {
        return stateInfo.fullPathHash == m_LocomotionPivotLId || 
               stateInfo.fullPathHash == m_LocomotionPivotRId || 
               transInfo.fullPathHash == m_LocomotionPivotLTransId || 
               transInfo.fullPathHash == m_LocomotionPivotRTransId;
    }

    public bool IsInIdle() // Is the character in Idle state?
    {
        return _animator.GetBool(m_IdleId);
    }

    public bool IsInLocomotion() // Is the character in Locomotion state?
    { return stateInfo.fullPathHash == m_LocomotionId; }

    public bool IsInLocomotionFall() // Is the character in Locomotion Fall state?
    { return stateInfo.fullPathHash == m_LocomotionFallId; }
    
    private void InputToWorldPos(ref float speedOut, ref Vector3 moveDirection)   //Converts Input Into World Positions
    {
        Vector3 playerDirection = transform.forward;
        Vector3 inputDirection = new Vector3(playerInput.x, 0, playerInput.y);
        inputDirection =Vector3.ClampMagnitude(inputDirection, 1f);

        speedOut = Mathf.SmoothDamp(speedOut,inputDirection.magnitude, ref _speedVelocity, speedSmoothTime);

        Vector3 cameraLookPos = playerCamera.transform.forward;
        cameraLookPos.y = 0.0f;
        
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(cameraLookPos));
        
        moveDirection = referentialShift * inputDirection;
        Vector3 axisSign = Vector3.Cross(moveDirection, playerDirection);

        //Debug.DrawRay(new Vector3(playerPos.x, playerPos.y + 2f, playerPos.z), inputDirection, Color.green);
        //Debug.DrawRay(new Vector3(playerPos.x, playerPos.y + 2f, playerPos.z), playerDirection, Color.red);
        //Debug.DrawRay(new Vector3(playerPos.x, playerPos.y + 2f, playerPos.z), moveDirection, Color.blue);
        //Debug.DrawRay(new Vector3(playerPos.x, playerPos.y + 2.5f, playerPos.z), axisSign, Color.magenta);
            
        float angleRootToMove = Vector3.Angle(playerDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);

        if (!IsInPivot())
        { charAngle = angleRootToMove; }

        angleRootToMove /= 180;
        
        
        direction = Mathf.SmoothDamp(direction,angleRootToMove * 1.5f, ref _turnVelocity, turnSmoothTime);
    }
    #endregion
}
