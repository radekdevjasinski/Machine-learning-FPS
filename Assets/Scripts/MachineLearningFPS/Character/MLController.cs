using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

using MachineLearningFPS.WeaponSystem;
using MachineLearningFPS.Environment;
using System.Collections.Generic;
using System;
using System.Collections;

namespace MachineLearningFPS.Character
{
    [RequireComponent(typeof(FPSMovement))]
    public class MLController : Agent
    {
        [Header("Episode Controller Reference")]
        [SerializeField] private EpisodeController _episodeController;
        private bool _firstSightRewardGiven = false;

        [Header("Agent Look Settings")]
        [SerializeField] private float _agentLookSensitivity = 1f;

        [SerializeField] private int _teamID = 0;
        public int TeamID => _teamID;

        private FPSMovement _movementBody;
        private CharacterController _characterController;
        private WeaponController _weaponController;
        private IInputProvider _inputProvider;
        public WeaponController Weapon => _weaponController;
        private bool _isSensorDataStale = false;
        private Vector2 _currentAgentMoveInput;
        private Vector2 _targetAgentLookInput;
        private bool _currentAgentJump;
        private bool _currentAgentCrouch;

        public event Action OnEpisodeEnded;
        public event Action OnAgentShot;
        public event Action OnAgentKilledEnemy;
        public event Action OnAgentDied;

        public override void Initialize()
        {
            _movementBody = GetComponent<FPSMovement>();
            _characterController = GetComponent<CharacterController>();
            _weaponController = GetComponentInChildren<WeaponController>();
            _inputProvider = GetComponent<IInputProvider>();
            _weaponController.SetAimTransform(_movementBody.HeadTransform);

        }

        public override void OnEpisodeBegin()
        {
            OnEpisodeEnded?.Invoke();
            _firstSightRewardGiven = false;
            _isSensorDataStale = true;
            base.OnEpisodeBegin();

        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];
            float lookX = actions.ContinuousActions[2];
            float lookY = _episodeController.Curriculum.EnableVerticalLooking ? actions.ContinuousActions[3] : 0f;

            _currentAgentJump = actions.DiscreteActions[0] > 0;
            bool shoot = actions.DiscreteActions[1] > 0;
            _currentAgentCrouch = actions.DiscreteActions[2] > 0;

            _currentAgentMoveInput = new Vector2(moveX, moveZ);
            _targetAgentLookInput = new Vector2(lookX, lookY);

            if (shoot)
            {
                if (_weaponController.Shoot())
                {
                    OnAgentShot?.Invoke();
                    bool enemyHit = CheckHitEnemy();
                    if (enemyHit && _episodeController.Curriculum.EnableGoodShootReward)
                    {
                        AddReward(_episodeController.Curriculum.RewardForGoodShoot);
                    }
                    else if (!enemyHit && _episodeController.Curriculum.EnableBadShootPenalty)
                    {
                        AddReward(_episodeController.Curriculum.PenaltyForBadShoot);
                    }
                }
            }

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
            // ML-Agents updates sensors right before calling CollectObservations. 
            _isSensorDataStale = false;

            // Own local velocity
            Vector3 localVelocity = transform.InverseTransformDirection(_characterController.velocity);
            sensor.AddObservation(localVelocity / Mathf.Max(_movementBody.MoveSpeed, 1f));

            // Is grounded
            bool isGrounded = _characterController.isGrounded;
            sensor.AddObservation(isGrounded);

            // Is crouching
            bool isCrouching = _characterController.height < 1.2f;
            sensor.AddObservation(isCrouching);

            // Weapon readiness
            float shootReadiness = _weaponController != null ? _weaponController.ShootReadinessPercentage : 0f;
            sensor.AddObservation(shootReadiness);

            // Equipped weapon one-hot
            int currentWeaponIndex = _weaponController != null ? _weaponController.CurrentWeaponIndex : -1;
            int weaponCount = _weaponController != null ? _weaponController.WeaponCount : 0;
            for (int i = 0; i < weaponCount; i++)
            {
                sensor.AddObservation(currentWeaponIndex == i);
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
            int weaponSelect = -1;

            if (_inputProvider != null)
            {
                moveInput = _inputProvider.MoveInput;
                lookInput = _inputProvider.LookInput;
                if (_episodeController.Curriculum.EnableJumping) jump = _inputProvider.JumpInput;
                if (_episodeController.Curriculum.EnableCrouching) crouch = _inputProvider.CrouchInput;
                shoot = _inputProvider.ShootInput;

                if (_episodeController.Curriculum.EnableWeaponSwapping)
                    weaponSelect = _inputProvider.WeaponSelectInput;
            }

            continuousActionsOut[0] = moveInput.x;
            continuousActionsOut[1] = moveInput.y;
            continuousActionsOut[2] = lookInput.x;
            continuousActionsOut[3] = _episodeController.Curriculum.EnableVerticalLooking ? lookInput.y : 0f;

            discreteActionsOut[0] = jump ? 1 : 0;
            discreteActionsOut[1] = shoot ? 1 : 0;
            discreteActionsOut[2] = crouch ? 1 : 0;
            discreteActionsOut[3] = weaponSelect >= 0 ? weaponSelect + 1 : 0; // +1 because 0 means "no change"

        }

        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            if (!_episodeController.Curriculum.EnableJumping)
            {
                actionMask.SetActionEnabled(0, 1, false);
            }

            if (!_episodeController.Curriculum.EnableCrouching)
            {
                actionMask.SetActionEnabled(2, 1, false);
            }

            if (!_episodeController.Curriculum.EnableWeaponSwapping)
            {
                int weaponCount = _weaponController != null ? _weaponController.WeaponCount : 0;
                for (int i = 1; i <= weaponCount; i++)
                {
                    actionMask.SetActionEnabled(3, i, false);
                }
            }
        }

        private void Update()
        {
            if (_movementBody != null)
            {
                Vector2 finalLookInput = _targetAgentLookInput * _agentLookSensitivity;

                _movementBody.SetInput(
                    _currentAgentMoveInput,
                    finalLookInput,
                    _currentAgentJump,
                    _currentAgentCrouch
                );

                _currentAgentJump = false;
            }

            if (!_isSensorDataStale)
            {
                CalculateEnvironmentRewards();
            }
        }

        private void CalculateEnvironmentRewards()
        {
            bool centerRayHit = CheckHitEnemy();
            if (centerRayHit && _episodeController.Curriculum.EnableFirstSightReward && !_firstSightRewardGiven)
            {
                AddReward(_episodeController.Curriculum.RewardForFirstSight);
                _firstSightRewardGiven = true;
            }
            if (centerRayHit && _episodeController.Curriculum.EnableLookingAtEnemyReward)
            {
                AddReward(_episodeController.Curriculum.RewardForLookingAtEnemy * Time.deltaTime);
            }
            ApplyContinuousRewards();
        }

        private void ApplyContinuousRewards()
        {
            if (_episodeController.Curriculum.EnableExistancePenalty)
            {
                AddReward(_episodeController.Curriculum.ExistancePenaltyAmount * Time.deltaTime);
            }
        }

        private bool CheckHitEnemy()
        {
            if (_movementBody == null || _movementBody.HeadTransform == null) return false;

            float rayDistance = _weaponController != null ? _weaponController.CurrentWeaponRange : 0f;
            int layerMask = LayerMask.GetMask("Player", "Default");

            if (Physics.Raycast(_movementBody.HeadTransform.position, _movementBody.HeadTransform.forward, out RaycastHit hit, rayDistance, layerMask))
            {
                if (hit.collider.CompareTag("Player") && hit.collider.gameObject != this.gameObject)
                {
                    return true;
                }
            }
            return false;
        }
        public void ApplyKillReward()
        {
            AddReward(_episodeController.Curriculum.KillRewardAmount);
            OnAgentKilledEnemy?.Invoke();
        }
        public void ApplyDeathPenalty()
        {
            //AddReward(-10f);
            OnAgentDied?.Invoke();
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!_episodeController.Curriculum.EnableWallHitPenalty) return;
            if (hit.gameObject.CompareTag("Obstacle"))
            {
                if (Mathf.Abs(hit.normal.y) < 0.2f)
                {
                    AddReward(_episodeController.Curriculum.PenaltyForWallHit * Time.deltaTime);
                }
            }
        }
    }

}
