using UnityEngine;

namespace MachineLearningFPS.Character
{
    [RequireComponent(typeof(CharacterController))]
    public class FPSMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpHeight = 1.5f;
        [SerializeField] private float _jumpCooldown = 1f;
        [SerializeField] private float _backwardSpeedMultiplier = 0.5f;
        [SerializeField] private float _strafeSpeedMultiplier = 0.7f;
        static readonly float GRAVITY = -9.81f;

        [Header("Crouch Settings")]
        [SerializeField] private float _crouchHeight = 1f;
        [SerializeField] private float _crouchSpeedMultiplier = 0.5f;
        [SerializeField] private float _crouchTransitionSpeed = 10f;
        [SerializeField] private float _crouchHeadY = 0.3f;
        [SerializeField] private float _crouchCooldown = 0.3f;

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
        private float _nextCrouchTime = 0f;

        private bool _isCrouching = false;
        private bool _wantsToCrouch = false;
        private bool _wasCrouchInput = false;

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

        public void ResetMovement()
        {
            _velocity = Vector3.zero;
            _currentMoveInput = Vector2.zero;
            _currentLookInput = Vector2.zero;
            _currentJumpInput = false;
            _currentCrouchInput = false;
            _xRotation = 0f;
            _headTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            _isCrouching = false;
            _wantsToCrouch = false;
            _wasCrouchInput = false;

            _nextJumpTime = 0f;
            _nextCrouchTime = 0f;
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
            if (_currentCrouchInput && !_wasCrouchInput && Time.time >= _nextCrouchTime)
            {
                _wantsToCrouch = !_wantsToCrouch;
                _nextCrouchTime = Time.time + _crouchCooldown;
            }
            _wasCrouchInput = _currentCrouchInput;

            bool blocked = !_wantsToCrouch && _isCrouching && !CanStandUp();
            _isCrouching = _wantsToCrouch || blocked;

            float targetHeight = _isCrouching ? _crouchHeight : _standingHeight;
            float currentSpeed = _isCrouching ? _moveSpeed * _crouchSpeedMultiplier : _moveSpeed;

            _controller.height = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * _crouchTransitionSpeed);

            float heightDifference = _standingHeight - _controller.height;
            _controller.center = new Vector3(0, _standingCenterY - (heightDifference / 2f), 0);

            if (_headTransform != null)
            {
                float targetHeadY = _isCrouching ? _crouchHeadY : _standingHeadY;
                Vector3 headPos = _headTransform.localPosition;
                headPos.y = Mathf.Lerp(headPos.y, targetHeadY, Time.deltaTime * _crouchTransitionSpeed);
                _headTransform.localPosition = headPos;
            }

            // --- 3. SKAKANIE I GRAWITACJA ---
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            if (_currentJumpInput && _controller.isGrounded && !_isCrouching && Time.time >= _nextJumpTime)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * GRAVITY);
                _nextJumpTime = Time.time + _jumpCooldown;
            }

            _currentJumpInput = false;

            // --- 4. PORUSZANIE SIĘ ---
            float adjustedInputX = _currentMoveInput.x * _strafeSpeedMultiplier;
            float adjustedInputY = _currentMoveInput.y < 0 ? _currentMoveInput.y * _backwardSpeedMultiplier : _currentMoveInput.y;

            Vector3 moveDirection = transform.right * adjustedInputX + transform.forward * adjustedInputY;

            if (moveDirection.magnitude > 1f)
            {
                moveDirection.Normalize();
            }

            _velocity.y += GRAVITY * Time.deltaTime;

            Vector3 finalMovement = (moveDirection * currentSpeed) + _velocity;

            _controller.Move(finalMovement * Time.deltaTime);
        }
        private bool CanStandUp()
        {
            float checkRadius = _controller.radius * 0.9f;
            Vector3 p1 = transform.TransformPoint(_controller.center + Vector3.up * (_controller.height / 2f - checkRadius));
            Vector3 p2 = transform.TransformPoint(new Vector3(0, _standingCenterY + (_standingHeight / 2f) - checkRadius, 0));

            int layerMask = ~LayerMask.GetMask("Player"); // Ignore players, check against everything else

            return !Physics.CheckCapsule(p1, p2, checkRadius, layerMask, QueryTriggerInteraction.Ignore);
        }
    }
}
