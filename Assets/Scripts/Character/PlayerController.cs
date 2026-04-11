using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FPSMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference fireAction;
    public InputActionReference crouchAction;

    [Header("References")]
    private FPSMovement _movementBody;
    [SerializeField] private LaserWeapon _laserWeapon;

    void Awake()
    {
        _movementBody = GetComponent<FPSMovement>();
        _laserWeapon = GetComponentInChildren<LaserWeapon>();
        moveAction?.action.Enable();
        lookAction?.action.Enable();
        jumpAction?.action.Enable();
        fireAction?.action.Enable();
        crouchAction?.action.Enable();
    }

    void Update()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        bool jump = jumpAction.action.IsPressed();
        bool crouch = crouchAction.action.IsPressed();

        _movementBody.SetInput(moveInput, lookInput, jump, crouch);
        if (fireAction.action.triggered)
        {
            _laserWeapon.Shoot();
        }
    }
}