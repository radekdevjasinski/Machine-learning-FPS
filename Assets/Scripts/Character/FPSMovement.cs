using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float jumpCooldown = 1f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public Transform cameraTransform;
    public float lookSpeed = 2f;
    public float maxLookAngle = 85f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    private Vector2 currentMoveInput;
    private Vector2 currentLookInput;
    private bool currentJumpInput;
    private float nextJumpTime = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void SetInput(Vector2 moveInput, Vector2 lookInput, bool jumpInput)
    {
        this.currentMoveInput = moveInput;
        this.currentLookInput = lookInput;
        this.currentJumpInput = jumpInput;
    }

    void Update()
    {
        // rozgladanie się
        transform.Rotate(Vector3.up * currentLookInput.x * lookSpeed);

        xRotation -= currentLookInput.y * lookSpeed;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        currentLookInput = Vector2.zero;

        // skakanie i grawitacja
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (currentJumpInput && controller.isGrounded && Time.time >= nextJumpTime)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            nextJumpTime = Time.time + jumpCooldown;
        }

        currentJumpInput = false;

        // poruszanie się
        Vector3 moveDirection = transform.right * currentMoveInput.x + transform.forward * currentMoveInput.y;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        velocity.y += gravity * Time.deltaTime;

        // wykonanie ruchu
        Vector3 finalMovement = (moveDirection * moveSpeed) + velocity;

        controller.Move(finalMovement * Time.deltaTime);
    }
}