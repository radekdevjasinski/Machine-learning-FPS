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

    private FPSMovement movementBody;

    public override void Initialize()
    {
        movementBody = GetComponent<FPSMovement>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        float lookX = actions.ContinuousActions[2];
        float lookY = actions.ContinuousActions[3];

        bool jump = actions.DiscreteActions[0] > 0;


        movementBody.SetInput(
            new Vector2(moveX, moveZ),
            new Vector2(lookX, lookY),
            jump
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        Vector2 moveInput = Vector2.zero;
        Vector2 lookInput = Vector2.zero;
        bool jump = false;


        if (moveAction != null) moveInput = moveAction.action.ReadValue<Vector2>();
        if (lookAction != null) lookInput = lookAction.action.ReadValue<Vector2>();
        if (jumpAction != null) jump = jumpAction.action.IsPressed();

        continuousActionsOut[0] = moveInput.x;
        continuousActionsOut[1] = moveInput.y;
        continuousActionsOut[2] = lookInput.x;
        continuousActionsOut[3] = lookInput.y;

        discreteActionsOut[0] = jump ? 1 : 0;

    }
}