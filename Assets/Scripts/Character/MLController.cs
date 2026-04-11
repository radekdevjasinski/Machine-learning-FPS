using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FPSMovement))]
public class MLController : Agent
{
    [Header("Input Actions (For Heuristic Only)")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference shootAction;
    public InputActionReference crouchAction;

    private FPSMovement movementBody;
    private LaserWeapon laserWeapon;

    public override void Initialize()
    {
        movementBody = GetComponent<FPSMovement>();
        laserWeapon = GetComponentInChildren<LaserWeapon>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        moveAction?.action.Enable();
        lookAction?.action.Enable();
        jumpAction?.action.Enable();
        shootAction?.action.Enable();
        crouchAction?.action.Enable();
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        float lookX = actions.ContinuousActions[2];
        float lookY = actions.ContinuousActions[3];

        bool jump = actions.DiscreteActions[0] > 0;
        bool shoot = actions.DiscreteActions[1] > 0;
        bool crouch = actions.DiscreteActions[2] > 0;


        movementBody.SetInput(
            new Vector2(moveX, moveZ),
            new Vector2(lookX, lookY),
            jump,
            crouch
        );

        if (shoot) laserWeapon.Shoot();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        Vector2 moveInput = Vector2.zero;
        Vector2 lookInput = Vector2.zero;
        bool jump = false;
        bool shoot = false;
        bool crouch = false;


        if (moveAction != null) moveInput = moveAction.action.ReadValue<Vector2>();
        if (lookAction != null) lookInput = lookAction.action.ReadValue<Vector2>();
        if (jumpAction != null) jump = jumpAction.action.IsPressed();
        if (shootAction != null) shoot = shootAction.action.IsPressed();
        if (crouchAction != null) crouch = crouchAction.action.IsPressed();

        continuousActionsOut[0] = moveInput.x;
        continuousActionsOut[1] = moveInput.y;
        continuousActionsOut[2] = lookInput.x;
        continuousActionsOut[3] = lookInput.y;

        discreteActionsOut[0] = jump ? 1 : 0;
        discreteActionsOut[1] = shoot ? 1 : 0;
        discreteActionsOut[2] = crouch ? 1 : 0;

    }
}