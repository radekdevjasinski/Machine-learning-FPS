using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _jumpCooldown = 1f;
    static readonly float GRAVITY = -9.81f;

    [Header("Crouch Settings")]
    [SerializeField] private float _crouchHeight = 1f;
    [SerializeField] private float _crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float _crouchTransitionSpeed = 10f;
    [SerializeField] private float _crouchHeadY = 0.3f;

    [Header("Look Settings")]
    [SerializeField] private float _lookSpeed = .1f;
    [SerializeField] private float _maxLookAngle = 60f;
    [SerializeField] private Transform _headTransform;

    private CharacterController _controller;
    private Vector3 _velocity;
    private float _xRotation = 0f;

    private float _standingHeight;
    private float _standingCenterY;
    private float _standingHeadY;

    private Vector2 _currentMoveInput;
    private Vector2 _currentLookInput;
    private bool _currentJumpInput;
    private bool _currentCrouchInput;

    public CharacterController Controller => _controller;
    public float StandingHeight => _standingHeight;
    public float MoveSpeed => _moveSpeed;
    public float MaxLookAngle => _maxLookAngle;
    public Transform HeadTransform => _headTransform;

    private float _nextJumpTime = 0f;

    void Start()
    {
        _controller = GetComponent<CharacterController>();

        _standingHeight = _controller.height;
        _standingCenterY = _controller.center.y;
        if (_headTransform != null)
        {
            _standingHeadY = _headTransform.localPosition.y;
        }
    }

    public void SetInput(Vector2 moveInput, Vector2 lookInput, bool jumpInput, bool crouchInput)
    {
        this._currentMoveInput = moveInput;
        this._currentLookInput = lookInput;
        this._currentJumpInput = jumpInput;
        this._currentCrouchInput = crouchInput;
    }

    void Update()
    {
        // --- 1. ROZGLĄDANIE SIĘ ---
        transform.Rotate(Vector3.up * _currentLookInput.x * _lookSpeed);

        _xRotation -= _currentLookInput.y * _lookSpeed;
        _xRotation = Mathf.Clamp(_xRotation, -_maxLookAngle, _maxLookAngle);
        _headTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        _currentLookInput = Vector2.zero;

        // --- 2. KUCANIE (Logika fizyczna) ---
        float targetHeight = _currentCrouchInput ? _crouchHeight : _standingHeight;
        float currentSpeed = _currentCrouchInput ? _moveSpeed * _crouchSpeedMultiplier : _moveSpeed;

        _controller.height = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * _crouchTransitionSpeed);

        float heightDifference = _standingHeight - _controller.height;
        _controller.center = new Vector3(0, _standingCenterY - (heightDifference / 2f), 0);

        if (_headTransform != null)
        {
            float targetHeadY = _currentCrouchInput ? _crouchHeadY : _standingHeadY;
            Vector3 headPos = _headTransform.localPosition;
            headPos.y = Mathf.Lerp(headPos.y, targetHeadY, Time.deltaTime * _crouchTransitionSpeed);
            _headTransform.localPosition = headPos;
        }

        // --- 3. SKAKANIE I GRAWITACJA ---
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        if (_currentJumpInput && _controller.isGrounded && !_currentCrouchInput && Time.time >= _nextJumpTime)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * GRAVITY);
            _nextJumpTime = Time.time + _jumpCooldown;
        }

        _currentJumpInput = false;

        // --- 4. PORUSZANIE SIĘ ---
        Vector3 moveDirection = transform.right * _currentMoveInput.x + transform.forward * _currentMoveInput.y;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        _velocity.y += GRAVITY * Time.deltaTime;

        Vector3 finalMovement = (moveDirection * currentSpeed) + _velocity;

        _controller.Move(finalMovement * Time.deltaTime);
    }
}