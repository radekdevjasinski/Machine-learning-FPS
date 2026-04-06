using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FPSAnimator : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;

    private Animator animator;
    private bool wasGrounded;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (controller != null)
        {
            wasGrounded = controller.isGrounded;
        }
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
    }
}