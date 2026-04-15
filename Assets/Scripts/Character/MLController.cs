using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
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
    private Health selfHealth;

    [Header("ML-Agents Settings")]
    [SerializeField] private Transform opponentTransform;
    [SerializeField] private float raycastDistance = 50f;
    [SerializeField] private LayerMask perceptionLayerMask;

    public override void Initialize()
    {
        movementBody = GetComponent<FPSMovement>();
        laserWeapon = GetComponentInChildren<LaserWeapon>();
        selfHealth = GetComponent<Health>();

        // This is a critical fix for the agent to shoot correctly.
        laserWeapon.SetAimTransform(movementBody.HeadTransform);
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

    public override void CollectObservations(VectorSensor sensor)
    {
        // --- Self Observations (8 observations) ---

        // 1. Self Rotation (1 float) - Pitch
        float currentPitch = movementBody.HeadTransform.localEulerAngles.x;
        if (currentPitch > 180) currentPitch -= 360;
        sensor.AddObservation(currentPitch / movementBody.MaxLookAngle);

        // 2. Self Velocity (3 floats) - Relative to agent's facing direction
        Vector3 localVelocity = transform.InverseTransformDirection(movementBody.Controller.velocity);
        sensor.AddObservation(localVelocity / movementBody.MoveSpeed);

        // 3. Self State (3 bools -> 3 floats)
        sensor.AddObservation(movementBody.Controller.isGrounded);
        sensor.AddObservation(movementBody.Controller.height < movementBody.StandingHeight); // Is Crouching
        sensor.AddObservation(laserWeapon.CanShoot());

        // 4. Self Health (1 float)
        sensor.AddObservation(selfHealth.currentHealth / selfHealth.maxHealth);

        // --- Perception Observations (14 rays * 3 obs/ray = 42 observations) ---
        // A fan of rays to understand the environment and locate the opponent.
        // Each ray provides: 1. Normalized distance, 2. Is it an opponent?, 3. Is it an obstacle?
        float[] rayAngles = { 0f, 20f, -20f, 45f, -45f, 70f, -70f, 90f, -90f, 135f, -135f, 180f }; // 12 horizontal rays
        Vector3[] raycastDirections = new Vector3[rayAngles.Length + 2]; // +2 for up and down

        for (int i = 0; i < rayAngles.Length; i++)
        {
            raycastDirections[i] = Quaternion.Euler(0, rayAngles[i], 0) * transform.forward;
        }
        raycastDirections[rayAngles.Length] = Vector3.up;
        raycastDirections[rayAngles.Length + 1] = Vector3.down;

        foreach (Vector3 dir in raycastDirections)
        {
            RaycastHit hit;
            if (Physics.Raycast(movementBody.HeadTransform.position, dir, out hit, raycastDistance, perceptionLayerMask))
            {
                bool isOpponent = (hit.collider.transform.root == opponentTransform);
                sensor.AddObservation(hit.distance / raycastDistance); // Normalized distance
                sensor.AddObservation(isOpponent);      // Is it opponent?
                sensor.AddObservation(!isOpponent);     // Is it obstacle?
            }
            else
            {
                // No hit
                sensor.AddObservation(1f);              // Max distance
                sensor.AddObservation(false);           // Not an opponent
                sensor.AddObservation(false);           // Not an obstacle
            }
        }
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