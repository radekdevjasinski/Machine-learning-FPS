using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

using MachineLearningFPS.WeaponSystem;
using MachineLearningFPS.UI;

namespace MachineLearningFPS.Character
{
    [RequireComponent(typeof(FPSMovement))]
    public class MLController : Agent
    {
        [Header("Input Actions (For Heuristic Only)")]
        public InputActionReference moveAction;
        public InputActionReference lookAction;
        public InputActionReference jumpAction;
        public InputActionReference shootAction;
        public InputActionReference crouchAction;
        public InputActionReference selectWeapon1;
        public InputActionReference selectWeapon2;
        public InputActionReference selectWeapon3;


        private FPSMovement _movementBody;
        private CharacterController _characterController;
        private WeaponController _weaponController;

        public override void Initialize()
        {
            _movementBody = GetComponent<FPSMovement>();
            _characterController = GetComponent<CharacterController>();
            _weaponController = GetComponentInChildren<WeaponController>();

            // This is a critical fix for the agent to shoot correctly.
            _weaponController.SetAimTransform(_movementBody.HeadTransform);
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

            _movementBody.SetInput(
                new Vector2(moveX, moveZ),
                new Vector2(lookX, lookY),
                jump,
                crouch
            );

            if (shoot) _weaponController.Shoot();

            if (actions.DiscreteActions[3] > 0)
            {
                int weaponIndex = actions.DiscreteActions[3] - 1;
                if (weaponIndex >= 0 && weaponIndex < _weaponController.WeaponCount)
                {
                    _weaponController.EquipWeapon(weaponIndex);
                }
            }

        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Own velocity (3 values)
            Vector3 velocity = _characterController.velocity;
            sensor.AddObservation(velocity.normalized);
            HUDConsole.Instance.UpdateValue("Velocity", velocity.magnitude);

            // Is grounded (1 value)
            bool isGrounded = _characterController.isGrounded;
            sensor.AddObservation(isGrounded);
            HUDConsole.Instance.UpdateValue("Grounded", isGrounded);

            // Is crouching (1 value)
            bool isCrouching = _characterController.height < 1.2f;
            sensor.AddObservation(isCrouching);
            HUDConsole.Instance.UpdateValue("Crouching", isCrouching);

            // Weapon readiness (1 value)
            float shootReadiness = _weaponController != null ? _weaponController.ShootReadinessPercentage : 0f;
            sensor.AddObservation(shootReadiness);
            HUDConsole.Instance.UpdateValue("Shoot Readiness", shootReadiness);

            // Equipped weapon one-hot (3 values)
            // This is better than a raw index because weapon type is categorical.
            int currentWeaponIndex = _weaponController != null ? _weaponController.CurrentWeaponIndex : -1;
            int weaponSlots = 3;
            for (int i = 0; i < weaponSlots; i++)
            {
                sensor.AddObservation(currentWeaponIndex == i);
                HUDConsole.Instance.UpdateValue($"Weapon {i + 1}", currentWeaponIndex == i);
            }

            // Raycast-based observations - what a human would see on screen
            // Cast rays from camera position to detect enemies in view
            Transform headTransform = _movementBody.HeadTransform;
            float maxRayDistance = 50f;
            Vector3 observedTargetDirection = Vector3.zero;
            float observedTargetDistance = 1f;
            bool hitEnemy = false;
            bool hitObstacle = false;

            // Cast rays in a cone pattern (center + 4 directions)
            Vector3[] rayDirections = new Vector3[]
            {
                headTransform.forward,
                Quaternion.Euler(-15, 0, 0) * headTransform.forward,
                Quaternion.Euler(15, 0, 0) * headTransform.forward,
                Quaternion.Euler(0, -15, 0) * headTransform.forward,
                Quaternion.Euler(0, 15, 0) * headTransform.forward,
            };

            foreach (Vector3 rayDirection in rayDirections)
            {
                RaycastHit hit;
                if (Physics.Raycast(headTransform.position, rayDirection, out hit, maxRayDistance))
                {
                    Health enemyHealth = hit.collider.GetComponent<Health>();
                    if (enemyHealth != null && hit.collider.gameObject != gameObject)
                    {
                        hitEnemy = true;
                        observedTargetDistance = hit.distance / maxRayDistance;
                        observedTargetDirection = (hit.point - headTransform.position).normalized;
                        break; // Take first enemy hit
                    }

                    // If ray hit a non-enemy object, record obstacle.
                    hitObstacle = true;
                    observedTargetDistance = hit.distance / maxRayDistance;
                    observedTargetDirection = (hit.point - headTransform.position).normalized;
                    break;
                }
            }
#if UNITY_EDITOR
            foreach (Vector3 rayDirection in rayDirections)
            {
                Debug.DrawRay(headTransform.position, rayDirection * maxRayDistance, Color.red);
            }
#endif

            // Raycast observations (6 values)
            sensor.AddObservation(hitEnemy); // 1 value
            HUDConsole.Instance.UpdateValue("Ray Hit Enemy", hitEnemy);
            sensor.AddObservation(hitObstacle); // 1 value
            HUDConsole.Instance.UpdateValue("Ray Hit Obstacle", hitObstacle);
            sensor.AddObservation(observedTargetDirection); // 3 values
            HUDConsole.Instance.UpdateValue("Observed Target Direction", observedTargetDirection);
            sensor.AddObservation(observedTargetDistance); // 1 value
            HUDConsole.Instance.UpdateValue("Observed Target Distance", observedTargetDistance);

            // Total observations: 1 + 3 + 1 + 1 + 1 + 1 + 3 + 1 = 15 observations
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
            int weaponSelect = -1;


            if (moveAction != null) moveInput = moveAction.action.ReadValue<Vector2>();
            if (lookAction != null) lookInput = lookAction.action.ReadValue<Vector2>();
            if (jumpAction != null) jump = jumpAction.action.IsPressed();
            if (shootAction != null) shoot = shootAction.action.IsPressed();
            if (crouchAction != null) crouch = crouchAction.action.IsPressed();
            if (selectWeapon1 != null && selectWeapon1.action.IsPressed()) weaponSelect = 0;
            if (selectWeapon2 != null && selectWeapon2.action.IsPressed()) weaponSelect = 1;
            if (selectWeapon3 != null && selectWeapon3.action.IsPressed()) weaponSelect = 2;

            continuousActionsOut[0] = moveInput.x;
            continuousActionsOut[1] = moveInput.y;
            continuousActionsOut[2] = lookInput.x;
            continuousActionsOut[3] = lookInput.y;

            discreteActionsOut[0] = jump ? 1 : 0;
            discreteActionsOut[1] = shoot ? 1 : 0;
            discreteActionsOut[2] = crouch ? 1 : 0;
            discreteActionsOut[3] = weaponSelect >= 0 ? weaponSelect + 1 : 0; // +1 because 0 means "no change"

        }
    }
}
