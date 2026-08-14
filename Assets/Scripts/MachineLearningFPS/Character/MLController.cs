using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
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
        public EpisodeController ActiveEpisodeController => _episodeController;

        [Header("Agent Look Settings")]
        [SerializeField] private float _agentLookSensitivity = 1f;
        private int _teamID;
        public int TeamID => _teamID;

        [Header("References")]
        private Health _health;
        private FPSMovement _movementBody;
        private CharacterController _characterController;
        private WeaponController _weaponController;
        private IInputProvider _inputProvider;
        private MLRewardManager _rewardManager;
        private BehaviorParameters _behaviorParameters;
        public WeaponController Weapon => _weaponController;

        [Header("Actions")]
        private Vector2 _currentAgentMoveInput;
        private Vector2 _targetAgentLookInput;
        private bool _currentAgentJump;
        private bool _currentAgentCrouch;

        public bool IsSensorDataStale { get; private set; } = false;
        public Vector3 LastKnownEnemyLocalDir => _rewardManager != null ? _rewardManager.LastKnownEnemyLocalDir : Vector3.zero;
        public float TimeSinceEnemySeen => _rewardManager != null ? _rewardManager.TimeSinceEnemySeen : 999f;
        public Health AgentHealth => _health;

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
            _health = GetComponent<Health>();
            _rewardManager = GetComponent<MLRewardManager>();
            _behaviorParameters = GetComponent<BehaviorParameters>();
            _weaponController.SetAimTransform(_movementBody.HeadTransform);

            // Single source of truth for team id: ML-Agents self-play already assigns this via
            // BehaviorParameters, mirroring it here avoids a second hand-set field drifting out of sync.
            if (_behaviorParameters != null) _teamID = _behaviorParameters.TeamId;

            if (_movementBody != null)
            {
                _movementBody.JumpPerformed += HandleJumpPerformed;
                _movementBody.CrouchPerformed += HandleCrouchPerformed;
            }
        }

        public override void OnEpisodeBegin()
        {
            OnEpisodeEnded?.Invoke();
            IsSensorDataStale = true;
            base.OnEpisodeBegin();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            IsSensorDataStale = false;

            // Own local velocity (3 floats)
            Vector3 localVelocity = transform.InverseTransformDirection(_characterController.velocity);
            sensor.AddObservation(localVelocity / Mathf.Max(_movementBody.MoveSpeed, 1f));

            // Current view direction (3 floats)
            sensor.AddObservation(_movementBody.HeadTransform.forward);

            // Is grounded (1 float)
            sensor.AddObservation(_characterController.isGrounded);

            // Is crouching (1 float)
            sensor.AddObservation(_characterController.height < 1.2f);

            // Weapon readiness (1 float)
            float shootReadiness = _weaponController != null
                ? _weaponController.ShootReadinessPercentage : 0f;
            sensor.AddObservation(shootReadiness);

            // Equipped weapon one-hot (weaponCount floats)
            int currentWeaponIndex = _weaponController != null
                ? _weaponController.CurrentWeaponIndex : -1;
            int weaponCount = _weaponController != null
                ? _weaponController.WeaponCount : 0;
            for (int i = 0; i < weaponCount; i++)
                sensor.AddObservation(currentWeaponIndex == i);

            // Own health normalised (1 float)
            float normHealth = (_health != null)
                ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth)
                : 1f;
            sensor.AddObservation(normHealth);

            // Last known enemy direction in LOCAL space (3 floats)
            sensor.AddObservation(LastKnownEnemyLocalDir);

            // Time since enemy last seen, normalised 0-1 (1 float)
            sensor.AddObservation(Mathf.Clamp01(TimeSinceEnemySeen / 5f));
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];
            float lookX = actions.ContinuousActions[2];
            float lookY = 0f;

            if (_episodeController == null)
                lookY = actions.ContinuousActions[3];
            else
                lookY = _episodeController.Curriculum.EnableVerticalLooking
                    ? actions.ContinuousActions[3] : 0f;

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
                }
            }

            if (actions.DiscreteActions[3] > 0)
            {
                int weaponIndex = actions.DiscreteActions[3] - 1;
                if (weaponIndex >= 0 && weaponIndex < _weaponController.WeaponCount)
                    _weaponController.EquipWeapon(weaponIndex);
            }
        }

        private void Update()
        {
            if (_movementBody != null)
            {
                _movementBody.SetInput(
                    _currentAgentMoveInput,
                    _targetAgentLookInput * _agentLookSensitivity,
                    _currentAgentJump,
                    _currentAgentCrouch);
                _currentAgentJump = false;
            }
        }

        private void OnDestroy()
        {
            if (_movementBody != null)
            {
                _movementBody.JumpPerformed -= HandleJumpPerformed;
                _movementBody.CrouchPerformed -= HandleCrouchPerformed;
            }
        }

        private void HandleJumpPerformed() => _rewardManager?.ApplyJumpPenalty();

        private void HandleCrouchPerformed() => _rewardManager?.ApplyCrouchingPenalty();

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
                if (_episodeController == null || _episodeController.Curriculum.EnableJumping)
                    jump = _inputProvider.JumpInput;
                if (_episodeController == null || _episodeController.Curriculum.EnableCrouching)
                    crouch = _inputProvider.CrouchInput;
                shoot = _inputProvider.ShootInput;
                if (_episodeController == null || _episodeController.Curriculum.EnableWeaponSwapping)
                    weaponSelect = _inputProvider.WeaponSelectInput;
            }

            continuousActionsOut[0] = moveInput.x;
            continuousActionsOut[1] = moveInput.y;
            continuousActionsOut[2] = lookInput.x;
            continuousActionsOut[3] = (_episodeController == null ||
                                       _episodeController.Curriculum.EnableVerticalLooking)
                ? lookInput.y : 0f;

            discreteActionsOut[0] = jump ? 1 : 0;
            discreteActionsOut[1] = shoot ? 1 : 0;
            discreteActionsOut[2] = crouch ? 1 : 0;
            discreteActionsOut[3] = weaponSelect >= 0 ? weaponSelect + 1 : 0;
        }

        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            if (_weaponController == null || _episodeController == null) return;

            if (!_episodeController.Curriculum.EnableJumping)
                actionMask.SetActionEnabled(0, 1, false);

            if (!_episodeController.Curriculum.EnableCrouching)
                actionMask.SetActionEnabled(2, 1, false);

            if (!_episodeController.Curriculum.EnableWeaponSwapping)
            {
                int wc = _weaponController != null ? _weaponController.WeaponCount : 0;
                for (int i = 1; i <= wc; i++)
                    actionMask.SetActionEnabled(3, i, false);
            }
        }

        public void ApplyKillReward() => _rewardManager?.ApplyKillReward();
        public void ApplyDeathPenalty() => _rewardManager?.ApplyDeathPenalty();
        public void ApplyKingOfTheHillReward(float rewardScale) => _rewardManager?.ApplyKingOfTheHillReward(rewardScale);
        public void ApplyTriggerReward(float rewardScale) => _rewardManager?.ApplyTriggerReward(rewardScale);
        public void ApplyWonByZoneReward() => _rewardManager?.ApplyWonByZoneReward();

        internal void NotifyAgentKilledEnemy() => OnAgentKilledEnemy?.Invoke();
        internal void NotifyAgentDied() => OnAgentDied?.Invoke();
        public void UpdateEnemyPositionFromDamage(Vector3 attackerPosition) => _rewardManager?.UpdateEnemyPositionFromDamage(attackerPosition);

    }
}