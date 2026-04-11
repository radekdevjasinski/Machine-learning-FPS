using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(Animator))]
public class CharacterVisuals : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;

    [Header("Crouch Settings")]
    [SerializeField] private float _heightToCrouchThreshold = 1.3f;
    [SerializeField] private float _crouchYOffset = -0.5f;
    [SerializeField] private float _crouchTransitionSpeed = 10f;
    [SerializeField] private MultiRotationConstraint _spineHunchConstraint;
    [SerializeField] private float _hunchMaxWeight = 0.7f;
    [SerializeField] private MultiAimConstraint _spineAimConstraint;
    [SerializeField] private float _spineAimMinWeight = 0.3f;
    [SerializeField] private float _hunchTransitionSpeed = 10f;
    [SerializeField] private Rig _legsRig;
    [SerializeField] private float _legsRigMaxWeight = 0.8f;

    private Animator animator;
    private bool wasGrounded;
    private float _defaultLocalY;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (controller != null)
        {
            wasGrounded = controller.isGrounded;
        }

        _defaultLocalY = transform.localPosition.y;
    }

    void Update()
    {
        if (controller == null) return;

        // speed
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        animator.SetFloat("Speed", currentSpeed, 0.05f, Time.deltaTime);
        animator.SetFloat("VerticalSpeed", controller.velocity.y);

        // jump
        bool isGrounded = controller.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        if (!isGrounded && wasGrounded && controller.velocity.y > 0f)
        {
            animator.SetTrigger("JumpStart");
        }
        else if (isGrounded && !wasGrounded)
        {
            animator.SetTrigger("JumpEnd");
        }
        wasGrounded = isGrounded;

        // crouch

        bool isCrouching = controller.height <= _heightToCrouchThreshold;
        float targetY = isCrouching ? _defaultLocalY + _crouchYOffset : _defaultLocalY;

        Vector3 currentLocalPos = transform.localPosition;
        currentLocalPos.y = Mathf.Lerp(currentLocalPos.y, targetY, Time.deltaTime * _crouchTransitionSpeed);
        transform.localPosition = currentLocalPos;

        if (_spineHunchConstraint != null)
        {
            float targetHunchWeight = isCrouching ? _hunchMaxWeight : 0f;
            _spineHunchConstraint.weight = Mathf.Lerp(_spineHunchConstraint.weight, targetHunchWeight, Time.deltaTime * _hunchTransitionSpeed);
        }

        if (_spineAimConstraint != null)
        {
            float targetAimWeight = isCrouching ? _spineAimMinWeight : 1f;
            _spineAimConstraint.weight = Mathf.Lerp(_spineAimConstraint.weight, targetAimWeight, Time.deltaTime * _hunchTransitionSpeed);
        }

        if (_legsRig != null)
        {
            float targetLegsRigWeight = isCrouching ? _legsRigMaxWeight : 0f;
            _legsRig.weight = Mathf.Lerp(_legsRig.weight, targetLegsRigWeight, Time.deltaTime * _hunchTransitionSpeed);
        }

    }
}