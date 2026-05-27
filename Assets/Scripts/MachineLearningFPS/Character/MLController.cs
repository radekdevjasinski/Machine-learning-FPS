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

        private Health _health;
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


        private Vector3 _lastKnownEnemyLocalDir = Vector3.zero;
        private float _timeSinceEnemySeen = 999f;

        public Vector3 LastKnownEnemyLocalDir => _lastKnownEnemyLocalDir;
        public float TimeSinceEnemySeen => _timeSinceEnemySeen;
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
            _weaponController.SetAimTransform(_movementBody.HeadTransform);
        }

        public override void OnEpisodeBegin()
        {
            OnEpisodeEnded?.Invoke();
            _firstSightRewardGiven = false;
            _isSensorDataStale = true;
            _lastKnownEnemyLocalDir = Vector3.zero;
            _timeSinceEnemySeen = 999f;
            base.OnEpisodeBegin();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            _isSensorDataStale = false;


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
            sensor.AddObservation(_lastKnownEnemyLocalDir);

            // Time since enemy last seen, normalised 0-1 (1 float)
            sensor.AddObservation(Mathf.Clamp01(_timeSinceEnemySeen / 5f));
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

                    bool enemyHit = IsEnemyHitByRaycast();

                    if (_episodeController == null) return;
                    if (enemyHit && _episodeController.Curriculum.EnableGoodShootReward)
                        AddReward(_episodeController.Curriculum.RewardForGoodShoot);
                    else if (!enemyHit && _episodeController.Curriculum.EnableBadShootPenalty)
                        AddReward(_episodeController.Curriculum.PenaltyForBadShoot);
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

            if (!_isSensorDataStale)
                CalculateEnvironmentRewards();
        }

        private void CalculateEnvironmentRewards()
        {
            if (_episodeController == null) return;

            Vector3 enemyWorldDir;
            bool enemyInSight = IsEnemyInSightWithDirection(_episodeController.Curriculum.AimingConeAngle, out enemyWorldDir);

            if (enemyInSight)
            {
                _lastKnownEnemyLocalDir = transform.InverseTransformDirection(enemyWorldDir);
                _timeSinceEnemySeen = 0f;

                if (_episodeController.Curriculum.EnableFirstSightReward && !_firstSightRewardGiven)
                {
                    AddReward(_episodeController.Curriculum.RewardForFirstSight);
                    _firstSightRewardGiven = true;
                }

                if (_episodeController.Curriculum.EnableLookingAtEnemyReward)
                    AddReward(_episodeController.Curriculum.RewardForLookingAtEnemy * Time.deltaTime);

                if (_episodeController.Curriculum.EnableApproachReward)
                {
                    ApplyApproachReward();
                }

                if (_episodeController.Curriculum.EnableAimingQualityReward)
                {
                    ApplyAimingReward(enemyWorldDir);
                }
            }
            else
            {
                _timeSinceEnemySeen += Time.deltaTime;
            }

            ApplyContinuousRewards();
        }

        private void ApplyApproachReward()
        {
            int playerMask = LayerMask.GetMask("Player");
            Collider[] nearby = Physics.OverlapSphere(
                transform.position, 50f, playerMask);

            foreach (var col in nearby)
            {
                if (col.gameObject == gameObject) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);


                if (distance > _episodeController.Curriculum.ApproachRewardMinDistance && distance < _episodeController.Curriculum.ApproachRewardMaxDistance)
                {
                    float normalizedDistance = 1f - (distance / _episodeController.Curriculum.ApproachRewardMaxDistance);
                    float approachReward = normalizedDistance * _episodeController.Curriculum.ApproachRewardScale * 0.001f;
                    AddReward(approachReward * Time.deltaTime);
                }
                break;
            }
        }

        private void ApplyAimingReward(Vector3 enemyWorldDir)
        {
            float dot = Vector3.Dot(_movementBody.HeadTransform.forward, enemyWorldDir);
            float coneEdge = Mathf.Cos(_episodeController.Curriculum.AimingConeAngle * Mathf.Deg2Rad);


            float aimQuality = Mathf.InverseLerp(coneEdge, 1f, dot);

            AddReward(aimQuality * _episodeController.Curriculum.AimingQualityRewardScale * Time.deltaTime);
        }

        private void ApplyContinuousRewards()
        {
            if (_episodeController == null) return;

            if (_episodeController.Curriculum.EnableExistancePenalty)
                AddReward(_episodeController.Curriculum.ExistancePenaltyAmount * Time.deltaTime);

            if (_episodeController.Curriculum.EnableContactPenalty)
                ApplyProximityContactPenalty();
        }


        private void ApplyProximityContactPenalty()
        {
            int playerLayerMask = LayerMask.GetMask("Player");
            Collider[] hits = Physics.OverlapSphere(
                transform.position, _episodeController.Curriculum.ContactDetectionRadius, playerLayerMask);
            foreach (Collider hit in hits)
            {
                if (hit.gameObject != gameObject)
                {
                    AddReward(_episodeController.Curriculum.ContactPenaltyAmount * Time.deltaTime);
                    break;
                }
            }
        }


        private bool IsEnemyInSight(float coneAngleDeg)
        {
            return IsEnemyInSightWithDirection(coneAngleDeg, out _);
        }

        private bool IsEnemyHitByRaycast()
        {
            if (_movementBody == null || _weaponController == null) return false;

            float rayDistance = _weaponController.CurrentWeaponRange;
            int layerMask = LayerMask.GetMask("Player", "Default");

            if (Physics.Raycast(_movementBody.HeadTransform.position, _movementBody.HeadTransform.forward, out RaycastHit hit, rayDistance, layerMask))
            {
                return hit.collider.CompareTag("Player") && hit.collider.gameObject != gameObject;
            }

            return false;
        }

        private bool IsEnemyInSightWithDirection(float coneAngleDeg, out Vector3 dirToEnemy)
        {
            dirToEnemy = Vector3.zero;
            if (_movementBody == null) return false;

            float rayDistance = _weaponController != null ?
                                _weaponController.CurrentWeaponRange : 0f;
            int playerMask = LayerMask.GetMask("Player");
            int layerMask = LayerMask.GetMask("Player", "Default");

            Collider[] nearby = Physics.OverlapSphere(
                _movementBody.HeadTransform.position, rayDistance, playerMask);

            foreach (var col in nearby)
            {
                if (col.gameObject == gameObject) continue;

                Vector3 targetPoint = col.bounds.center;
                MLController enemyAgent = col.GetComponentInParent<MLController>();
                if (enemyAgent != null)
                {
                    FPSMovement enemyMovement = enemyAgent.GetComponent<FPSMovement>();
                    if (enemyMovement != null && enemyMovement.HeadTransform != null)
                        targetPoint = enemyMovement.HeadTransform.position;
                }

                Vector3 dir = (targetPoint - _movementBody.HeadTransform.position).normalized;
                float angle = Vector3.Angle(_movementBody.HeadTransform.forward, dir);

                if (angle >= coneAngleDeg) continue;

                if (Physics.Raycast(_movementBody.HeadTransform.position, dir,
                    out RaycastHit hit, rayDistance, layerMask))
                {
                    if (hit.collider.CompareTag("Player") &&
                        hit.collider.gameObject != gameObject)
                    {
                        dirToEnemy = dir;
                        return true;
                    }
                }
            }
            return false;
        }

        public void ApplyKillReward()
        {
            if (_episodeController != null && _episodeController.Curriculum.EnableKillReward)
                AddReward(_episodeController.Curriculum.KillRewardAmount);
            OnAgentKilledEnemy?.Invoke();
        }

        public void ApplyDeathPenalty()
        {
            if (_episodeController != null && _episodeController.Curriculum.EnableDeathPenalty)
                AddReward(_episodeController.Curriculum.DeathPenaltyAmount);
            OnAgentDied?.Invoke();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_episodeController == null) return;
            if (_episodeController.Curriculum.EnableWallHitPenalty)
            {
                if (hit.gameObject.CompareTag("Obstacle") &&
                    Mathf.Abs(hit.normal.y) < 0.2f)
                {
                    AddReward(_episodeController.Curriculum.PenaltyForWallHit * Time.deltaTime);
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
    }
}