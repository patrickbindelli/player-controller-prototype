using System;
using UnityEngine;
using Unity.Mathematics;
using Sirenix.OdinInspector;
public class FlightController : MonoBehaviour
{
    [Title("Components")] 
    [ShowInInspector, ReadOnly]
    private Transform _character;
    [ShowInInspector, ReadOnly] 
    private Transform _camera;
    [ShowInInspector, ReadOnly]
    private Rigidbody _rb;
    [ShowInInspector, ReadOnly]
    private Animator _animator;
    [ShowInInspector, ReadOnly]
    private CharacterController _controller;
    [ShowInInspector, ReadOnly]
    private CapsuleCollider _collider;
    [ShowInInspector, ReadOnly]
    private PC2 _pc2;
    
    [Title("Hover Settings")]
    [SerializeField]
    private float strength;
    [SerializeField]
    private float length;
    [SerializeField]
    private float dampening;
    
    [Title("Flying Settings")]
    [SerializeField]
    private float flyUpSpeed = 5f;
    [SerializeField]
    private float flyForwardSpeed = 10f;
    [SerializeField]
    private float turnSmoothTime = .1f;
    [SerializeField]
    public float speedSmoothTime = .5f;
    [SerializeField]
    public float upSpeedSmoothTime = .75f;
    [ShowInInspector, ReadOnly]
    private bool _flying;
    
    
    // Private-Only
    private float _lastHitDist;
    
    private Vector3 _inputDir;
    private Vector3 _playerInput;
    private Vector3 _velocity;
    private bool _flyUpDownInput;
    private bool _flyUp;
    private bool _flyDown;
    
    private float _currentSpeed;
    private float _speedSmoothVelocity;
    private float _turnSmoothVelocity;
    private float _flySmoothVelocity;
    private float _layerSmoothVelocity;
    
    private float _layerWeight;
    private int _flyingIndex;
    private int _raycastState;
    
    // Hashes
    private int _mFlying;
    private int _mFlyingDirection;
    
    Vector3 startPos;
    public float amplitude = 10f;
    public float period = 5f;
    
    #region Unity Events

    void Awake()
    {
        GetAllComponents();  // -- remove for manually setting components
        SetHashes();
    }

    private void Start()
    {
        startPos = _character.position;
    }

    void FixedUpdate()
    {
        _velocity = _rb.velocity;
        
        RaycastHit hit;
        Ray ray = new Ray(_character.position, _character.TransformDirection(-Vector3.up));
        _raycastState = Physics.Raycast(ray, out hit, length) ? 1 : 0;

        switch (_raycastState)
        {
            case 0:
                _lastHitDist = length * 1.1f;
                break;
            case 1:
                float forceAmount = HooksLawDampen(hit.distance);
                _rb.AddForceAtPosition(_character.up * forceAmount, _character.position);
                break;
        }
        
        
        if (_character.position.y > length)
        {
            _rb.useGravity = false;
            _velocity.y = Mathf.SmoothDamp(_velocity.y, 0, ref _flySmoothVelocity, upSpeedSmoothTime );
        }

        if (_character.position.y <= length)
        {
            _velocity.y = Mathf.SmoothDamp(_velocity.y, _velocity.y < 0 ? 0 : _velocity.y, ref _flySmoothVelocity, .2f );
        }
        
        Move();

        _velocity.y = _flyUp ? FlyUp() : (_flyDown ? FlyDown() : _velocity.y);
        _rb.velocity = _velocity;
    }

    private void Update()
    { 
        // Player Inputs
        _playerInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxis("Vertical"));
        _flyUp = Input.GetButton("Jump");
        _flyDown = Input.GetKey(KeyCode.LeftControl);
        
        _inputDir = _playerInput.normalized;
        
        // Animator
        _animator.SetFloat(_mFlyingDirection, 0);
        _animator.SetBool(_mFlying, _flying);
        
        _flyingIndex = Input.GetKeyDown(KeyCode.F) ? 1 : 0;
        switch (_flyingIndex)
        { case 1:
                SetComponentsState();
                break; 
        }

        if(_flying && _inputDir != Vector3.zero)
        { 
            float targetRotation = Mathf.Atan2(_inputDir.x, _inputDir.y) * Mathf.Rad2Deg + _camera.eulerAngles.y;
            _character.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(_character.eulerAngles.y, targetRotation, ref _turnSmoothVelocity, turnSmoothTime);
        }
        
        SetLayerWeigh();
    }
    #endregion Unity Events
    

    #region Methods
    
    private void GetAllComponents() // Caches all used components automatically
    {
        _character = GetComponent<Transform>();
        _camera = Camera.main.transform;
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _collider = GetComponent<CapsuleCollider>();
        _pc2 = GetComponent<PC2>();
    }
    private void SetHashes() // Sets Hashes for performance
    {
        _mFlying = Animator.StringToHash("Flying");
        _mFlyingDirection = Animator.StringToHash("FlyDirection");
    }
    private void SetComponentsState() // Sets what it needs to be enabled or disabled for flying correctly
    {
        _controller.enabled = !_controller.enabled;
        _pc2.enabled = !_pc2.enabled;
        _animator.applyRootMotion = !_animator.applyRootMotion;
        _rb.isKinematic = !_rb.isKinematic;
        _flying = !_flying;
        _collider.enabled = !_collider.enabled;
    }
    private void Move() // Sets flying forward/backward flying velocity
    {
        float targetSpeed = flyForwardSpeed * _inputDir.magnitude;

        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedSmoothVelocity, speedSmoothTime );

        Vector3 playerVelocity = (_character.forward * _currentSpeed);
        
        _velocity.x = playerVelocity.x;
        _velocity.z = playerVelocity.z;
    }
    private float FlyUp()
    {
        float flyspeed = _flyUp ? flyUpSpeed : 0f;
        Debug.Log(flyspeed);
        return (Mathf.SmoothDamp(_velocity.y,flyspeed, ref _flySmoothVelocity, upSpeedSmoothTime));
        
    }
    private float FlyDown()
    {
        float downdspeed = _flyDown ? -flyUpSpeed: 0;
        Debug.Log("Down");
        return (Mathf.SmoothDamp(_velocity.y,downdspeed, ref _flySmoothVelocity, upSpeedSmoothTime));
    }
    private void SetLayerWeigh()
    {
        _layerWeight = Mathf.SmoothDamp(_layerWeight, _flying ? 1 : 0, ref _layerSmoothVelocity, .5f);
        _animator.SetLayerWeight(1, _layerWeight);
    }
    private float HooksLawDampen(float hitDistance) // Hook's Law Implementation, handles spring-like hover effect
    {
        float forceAmount = strength * (length - hitDistance) + (dampening * (_lastHitDist - hitDistance));
        forceAmount = Mathf.Max(0f,forceAmount);
        _lastHitDist = hitDistance;

        return forceAmount;
    }

    #endregion Methods
}
