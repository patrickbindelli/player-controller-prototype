using System;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

public class PC2 : MonoBehaviour
{
    [Title("Components")] 
    
    public Material Stealth;

    public float stealthFloat;
    private float _stealthFloatValue;
    [SerializeField]
    private Transform player;
    [SerializeField]
    private CharacterController controller;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Transform cameraT;
    [SerializeField]
    private Transform headComponent;

    [Title("Movement")]
    [SerializeField] 
    private float walkSpeed = 2f; // PLayer's velocity while walking
    [SerializeField] 
    private float runSpeed = 6f;  // PLayer's velocity while running
    
    [Title("Movement Smoothing", bold: false)]
    [SerializeField]
    private float turnSmoothTime = 0.2f;
    [SerializeField]
    private float speedSmoothTime = 0.1f;
    
    [Title("Jump")]
    
    [SerializeField] 
    private float gravity;
    [SerializeField] 
    private float jumpHeight = 1f;
    
    [Title("Jump Smoothing", bold: false)]
    [SerializeField]
    private float stepDownOffset = 0.3f;
    [SerializeField] 
    private float jumpDamp = 0.5f;
    [SerializeField] 
    private float airControl = 2.5f;

    //Private-Only
    private Vector2 _playerInput;
    private Vector2 _inputDir;

    private bool _running;
    private bool _jumping;
    private bool _crouching;

    private int _whenCrouch;

    private float _speedSmoothVelocity;
    private float _turnSmoothVelocity;
    private float _currentSpeed;
    private Vector3 _velocity;
    private float _direction;
    private float _directionTurnVelocity;
    private bool _isInAir;

    private float _charAngle;
    
    
    private AnimatorStateInfo _stateInfo;
    private AnimatorTransitionInfo _transInfo;
    
    // Hashes for performance
    private int _mSpeed;
    private int _mJumping;
    private int _mOnGround;
    private int _mOnAir;
    private int _mHeight;
    private int _mDirection;
    
    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;
    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;
   

    private Vector3 _moveDirection;

    
    private float LocomotionThreshold { get { return 0.2f; } }
    
    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        player = transform;
        cameraT = Camera.main.transform;
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        MakeHashes();
    }
    
    private void MakeHashes()
    {
        _mSpeed = Animator.StringToHash("Speed");
        _mJumping = Animator.StringToHash("Jumping");
        _mOnGround = Animator.StringToHash("OnGround");
        _mHeight = Animator.StringToHash("Height");
        _mOnAir = Animator.StringToHash("OnAir");
        _mDirection = Animator.StringToHash("Direction");
    }

    private void FixedUpdate()
    {
        if (_isInAir)
        {
            UpdateInAir();
        }else
        {
            UpdateOnGround();
        }
        
        //if(animator.enabled) animator.SetBool(_mOnGround, controller.isGrounded);
        
        Move();

    }
    
    private void Update()
    {
        GroundedCheck();
        // Get animator infos
        _stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        _transInfo = animator.GetAnimatorTransitionInfo(0);
        
        // Handle player input
        _playerInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxis("Vertical"));
        _inputDir = _playerInput.normalized;
        _running = Input.GetKey(KeyCode.LeftShift);
        _jumping = Input.GetKeyDown(KeyCode.Space);
        _crouching = Input.GetKey(KeyCode.LeftControl);

        _whenCrouch = _crouching ? 1 : 0;

        switch (_whenCrouch)
        {
            case 0:
                controller.center = new Vector3(0, 0.89f, 0);
                controller.height = 1.8f;
                stealthFloat = Mathf.SmoothDamp(stealthFloat, 0, ref _stealthFloatValue,.5f);
                Stealth.SetFloat("CutOffHeight", stealthFloat);
                break;
            case 1:
                controller.center = new Vector3(0, 0.7f, 0) ;
                controller.height = 1.4f;
                stealthFloat = Mathf.SmoothDamp(stealthFloat, 4f, ref _stealthFloatValue,.5f);
                Stealth.SetFloat("CutOffHeight", stealthFloat);
                
                
                break;
        }
        
        float animationSpeedPercent = ((_running) ? runSpeed : walkSpeed) * _inputDir.magnitude;

		if( _inputDir != Vector2.zero)
        { 
            float targetRotation = Mathf.Atan2(_inputDir.x, _inputDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            player.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(player.eulerAngles.y, targetRotation, ref _turnSmoothVelocity, turnSmoothTime);
        }
        
        // Animator
        animator.SetBool(_mOnAir, _isInAir);
        animator.SetFloat(_mSpeed, animationSpeedPercent, speedSmoothTime, Time.deltaTime);
        animator.SetBool("Crouching", _crouching);

        //animator.SetFloat(_mAngle, _charAngle);
        animator.SetFloat(_mDirection, _direction);
        
        CalculateAngleToMove(ref _direction, ref _charAngle);

        DetectWallCollision(ref animationSpeedPercent);
        DetectPlayerDistanceFromGround();
        
        Jump();
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        
        Debug.Log(Grounded);

        // update animator if using character
        animator.SetBool(_mOnGround, Grounded);
    }
    
    private void CalculateAngleToMove(ref float directionOut, ref float angleOut)
    {
        Vector3 playerDirection = player.forward;
        Vector3 inputDirection = new Vector3(_inputDir.x, 0, _inputDir.y);
        
        // Get camera rotation
        Vector3 cameraLookPos = Camera.main.transform.forward;
        cameraLookPos.y = 0.0f;
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(cameraLookPos));

        // Convert joystick input in Worldspace coordinates
        _moveDirection = referentialShift * inputDirection;
        Vector3 axisSign = Vector3.Cross(_moveDirection, playerDirection);
        
        //Debug.Log(axisSign);
        
        Debug.DrawRay(new Vector3(player.transform.position.x, player.transform.position.y + 2f, player.transform.position.z), _moveDirection.normalized, Color.green);
        Debug.DrawRay(new Vector3(player.transform.position.x, player.transform.position.y + 2f, player.transform.position.z), playerDirection.normalized, Color.red);

        //float angle = Vector3.Angle(playerDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);
        float angle = Mathf.Acos(Vector3.Dot(_moveDirection.normalized, playerDirection.normalized)) * Mathf.Rad2Deg * (axisSign.y >= 0 ? -1f : 1f);
        
        angle /= 180;

        directionOut = Mathf.SmoothDamp(directionOut, angle * 1.5f, ref _directionTurnVelocity, turnSmoothTime);
    }
    
    private void DetectPlayerDistanceFromGround()
    {
        RaycastHit hit;
        Ray height = new Ray(player.position, Vector3.down);
        Physics.Raycast(height, out hit);
        animator.SetFloat(_mHeight, hit.distance);
        Debug.DrawRay(height.origin, height.direction * 0.5f, Color.red);
    }

    private void DetectWallCollision(ref float speed)
    {
        Ray ray = new Ray(headComponent.position, headComponent.forward);
        if (Physics.Raycast(ray, .5f))
        {
            speed = 0;
        }
        
        Debug.DrawRay(ray.origin, ray.direction * .5f, Color.blue);
    }

    private void Move()
    {
        float targetSpeed = ((_running) ? runSpeed : walkSpeed) * _inputDir.magnitude;

        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedSmoothVelocity, speedSmoothTime );

        Vector3 playerVelocity = (player.forward * _currentSpeed);
        
        player.Translate(playerVelocity * Time.deltaTime);
        
    }

    private void UpdateOnGround()
    {
        Vector3 stepDownAmount = Vector3.down * stepDownOffset;
        controller.Move(stepDownAmount);
        animator.SetBool(_mJumping, false);
        
        if (!controller.isGrounded)
        {
            SetInAir(0);
        }
        
    }

    private void UpdateInAir()
    {
        _velocity.y -= gravity * Time.fixedDeltaTime;
        Vector3 displacement = _velocity * Time.fixedDeltaTime;
        displacement += CalculateAirControl();
        controller.Move(displacement);
        _isInAir = !controller.isGrounded;
    }
    
    private void Jump()
    {
        if (_jumping && !_isInAir)
        {
            animator.SetBool(_mJumping, true);
            float velocityY = Mathf.Sqrt(2 * gravity * jumpHeight);
            SetInAir(velocityY);
        }
    }

    private Vector3 CalculateAirControl()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxis("Vertical"));
        input.Normalize();
        
        return ((player.forward * input.y) + (player.right * input.x)) * (airControl / 100f);
    }
    private void SetInAir(float velocityY)
    {
        _isInAir = true;
        _velocity = animator.velocity * jumpDamp;
        _velocity.y = velocityY;
    }
}
