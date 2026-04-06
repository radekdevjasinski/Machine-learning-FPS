using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FPSMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;

    private FPSMovement movementBody;

    void Awake()
    {
        movementBody = GetComponent<FPSMovement>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
    }

    void Update()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        bool jump = jumpAction.action.IsPressed();

        movementBody.SetInput(moveInput, lookInput, jump);
    }
}