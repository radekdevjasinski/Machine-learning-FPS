using UnityEngine;
using Unity.MLAgents.Sensors;
using MachineLearningFPS.Environment;
using MachineLearningFPS.WeaponSystem;
using System.Collections;

namespace MachineLearningFPS.Character
{
    [RequireComponent(typeof(MLController), typeof(FPSMovement), typeof(CharacterController))]
    public class MLRewardManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _sensorParent;
        [SerializeField] private RayPerceptionSensorComponent3D[] _raySensors;

        private MLController _agent;
        private FPSMovement _movementBody;
        private CharacterController _characterController;
        private WeaponController _weaponController;

        private EpisodeController EpisodeController => _agent.ActiveEpisodeController;

        private bool _firstSightRewardGiven = false;

        private int _jumpSafeZoneCount = 0;
        private int _crouchSafeZoneCount = 0;

        public Vector3 LastKnownEnemyLocalDir { get; private set; } = Vector3.zero;
        public float TimeSinceEnemySeen { get; private set; } = 999f;


        private void Awake()
        {
            _agent = GetComponent<MLController>();
            _movementBody = GetComponent<FPSMovement>();
            _characterController = GetComponent<CharacterController>();
            _weaponController = GetComponentInChildren<WeaponController>();

            if (_sensorParent != null)
            {
                _raySensors = _sensorParent.GetComponentsInChildren<RayPerceptionSensorComponent3D>();
            }
        }

        private void OnEnable()
        {
            _agent.OnEpisodeEnded += ResetPerception;
            _agent.OnAgentShot += HandleAgentShot;
        }

        private void OnDisable()
        {
            _agent.OnEpisodeEnded -= ResetPerception;
            _agent.OnAgentShot -= HandleAgentShot;
        }

        private void ResetPerception()
        {
            _firstSightRewardGiven = false;
            LastKnownEnemyLocalDir = Vector3.zero;
            TimeSinceEnemySeen = 999f;

            // Failsafe clearing in case Physics teleportation disrupts OnTriggerExit
            _jumpSafeZoneCount = 0;
            _crouchSafeZoneCount = 0;
        }

        private void Update()
        {
            if (!_agent.IsSensorDataStale)
            {
                CalculateEnvironmentRewards();
            }
        }
        public void UpdateEnemyPositionFromDamage(Vector3 attackerPosition)
        {
            Transform referenceTransform = _movementBody != null && _movementBody.HeadTransform != null
                ? _movementBody.HeadTransform
                : transform;

            Vector3 directionToAttacker = attackerPosition - referenceTransform.position;
            if (directionToAttacker.sqrMagnitude > 1e-6f)
            {
                LastKnownEnemyLocalDir = referenceTransform.InverseTransformDirection(directionToAttacker.normalized);
            }
            else
            {
                LastKnownEnemyLocalDir = Vector3.zero;
            }

            TimeSinceEnemySeen = 0f;
        }

        private void CalculateEnvironmentRewards()
        {
            bool enemyInSight = IsEnemyInSightFromSensor(out Vector3 enemyWorldDir, out float bestAimQualityDot);

            if (enemyInSight)
            {
                LastKnownEnemyLocalDir = transform.InverseTransformDirection(enemyWorldDir);
                TimeSinceEnemySeen = 0f;
            }
            else
            {
                TimeSinceEnemySeen += Time.deltaTime;
            }

            if (EpisodeController != null)
            {
                if (enemyInSight)
                {
                    if (EpisodeController.Curriculum.EnableFirstSightReward && !_firstSightRewardGiven)
                    {
                        _agent.AddReward(EpisodeController.Curriculum.RewardForFirstSight);
                        _firstSightRewardGiven = true;
                    }

                    if (EpisodeController.Curriculum.EnableLookingAtEnemyReward)
                        _agent.AddReward(EpisodeController.Curriculum.RewardForLookingAtEnemy * Time.deltaTime);

                    if (EpisodeController.Curriculum.EnableApproachReward)
                        ApplyApproachReward();

                    if (EpisodeController.Curriculum.EnableAimingQualityReward)
                        ApplyAimingReward(bestAimQualityDot);
                }

                //bool isJumping = !_characterController.isGrounded;
                //bool isCrouching = _characterController.height < 1.3f;
                //ApplyActionPenalties(isJumping, isCrouching);

                ApplyContinuousRewards();
            }
        }

        private void ApplyApproachReward()
        {
            int playerMask = LayerMask.GetMask("Player");
            Collider[] nearby = Physics.OverlapSphere(transform.position, 50f, playerMask);

            foreach (var col in nearby)
            {
                if (col.gameObject == gameObject) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);

                if (distance > EpisodeController.Curriculum.ApproachRewardMinDistance && distance < EpisodeController.Curriculum.ApproachRewardMaxDistance)
                {
                    float normalizedDistance = 1f - (distance / EpisodeController.Curriculum.ApproachRewardMaxDistance);
                    float approachReward = normalizedDistance * EpisodeController.Curriculum.ApproachRewardScale * 0.001f;
                    _agent.AddReward(approachReward * Time.deltaTime);
                }
                break;
            }
        }

        private void ApplyAimingReward(float aimQualityDot)
        {
            float coneEdge = Mathf.Cos(EpisodeController.Curriculum.AimingConeAngle * Mathf.Deg2Rad);
            float aimQuality = Mathf.InverseLerp(coneEdge, 1f, aimQualityDot);
            _agent.AddReward(aimQuality * EpisodeController.Curriculum.AimingQualityRewardScale * Time.deltaTime);
        }

        private void ApplyContinuousRewards()
        {
            if (EpisodeController == null) return;

            if (EpisodeController.Curriculum.EnableExistancePenalty)
                _agent.AddReward(EpisodeController.Curriculum.ExistancePenaltyAmount * Time.deltaTime);

            if (EpisodeController.Curriculum.EnableContactPenalty)
                ApplyProximityContactPenalty();
        }

        private void ApplyProximityContactPenalty()
        {
            int playerLayerMask = LayerMask.GetMask("Player");
            Collider[] hits = Physics.OverlapSphere(transform.position, EpisodeController.Curriculum.ContactDetectionRadius, playerLayerMask);
            foreach (Collider hit in hits)
            {
                if (hit.gameObject != gameObject)
                {
                    _agent.AddReward(EpisodeController.Curriculum.ContactPenaltyAmount * Time.deltaTime);
                    break;
                }
            }
        }

        public void ApplyKingOfTheHillReward(float rewardScale)
        {
            if (EpisodeController != null && EpisodeController.Curriculum.EnableKingOfTheHillReward)
                _agent.AddReward(EpisodeController.Curriculum.KingOfTheHillRewardAmount * rewardScale * Time.fixedDeltaTime);
        }

        private void HandleAgentShot()
        {
            if (EpisodeController == null) return;

            bool enemyHit = IsEnemyHitByRaycast();

            if (enemyHit && EpisodeController.Curriculum.EnableGoodShootReward)
                _agent.AddReward(EpisodeController.Curriculum.RewardForGoodShoot);
            else if (!enemyHit && EpisodeController.Curriculum.EnableBadShootPenalty)
                _agent.AddReward(EpisodeController.Curriculum.PenaltyForBadShoot);
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

        private bool IsEnemyInSightFromSensor(out Vector3 bestEnemyWorldDir, out float bestAimQualityDot)
        {
            bestEnemyWorldDir = Vector3.zero;
            bestAimQualityDot = -1f;

            if (_raySensors == null || _raySensors.Length == 0)
            {
                Debug.LogWarning("[MLRewardManager] No ray sensors assigned for enemy detection.");
                return false;
            }

            bool foundEnemy = false;

            foreach (var sensor in _raySensors)
            {
                if (sensor == null) continue;

                var rayInput = sensor.GetRayPerceptionInput();
                var rayOutput = RayPerceptionSensor.Perceive(rayInput, false);
                Transform sensorTransform = sensor.transform;

                foreach (var hit in rayOutput.RayOutputs)
                {
                    if (hit.HasHit && hit.HitGameObject != null)
                    {
                        if (hit.HitGameObject.CompareTag("Player") && hit.HitGameObject != gameObject)
                        {
                            foundEnemy = true;

                            Vector3 targetPoint = hit.HitGameObject.transform.position;
                            Collider col = hit.HitGameObject.GetComponentInChildren<Collider>();
                            if (col != null) targetPoint = col.bounds.center;

                            Vector3 dirToTarget = (targetPoint - sensorTransform.position).normalized;
                            float dot = Vector3.Dot(sensorTransform.forward, dirToTarget);

                            if (dot > bestAimQualityDot)
                            {
                                bestAimQualityDot = dot;
                                bestEnemyWorldDir = dirToTarget;
                            }
                        }
                    }
                }
            }

            return foundEnemy;
        }

        public void ApplyKillReward()
        {
            if (EpisodeController != null && EpisodeController.Curriculum.EnableKillReward)
                _agent.AddReward(EpisodeController.Curriculum.KillRewardAmount);
            _agent.NotifyAgentKilledEnemy();
        }

        public void ApplyWonByZoneReward()
        {
            if (EpisodeController != null && EpisodeController.Curriculum.EnableWonByZoneReward)
                _agent.AddReward(EpisodeController.Curriculum.WonByZoneRewardAmount);
        }

        public void ApplyDeathPenalty()
        {
            if (EpisodeController != null && EpisodeController.Curriculum.EnableDeathPenalty)
                _agent.AddReward(EpisodeController.Curriculum.DeathPenaltyAmount);
            _agent.NotifyAgentDied();
        }

        public void ApplyWeaponChangePenalty()
        {
            if (EpisodeController != null && EpisodeController.Curriculum.EnableWeaponChangePenalty)
                _agent.AddReward(EpisodeController.Curriculum.WeaponChangePenaltyAmount);
        }

        public void ApplyTriggerReward(float rewardScale)
        {
            if (EpisodeController != null && EpisodeController.Curriculum.EnableTriggerReward)
                _agent.AddReward(EpisodeController.Curriculum.TriggerRewardAmount * rewardScale);
        }

        public void ChangeJumpSafeZoneCount(int amount) => _jumpSafeZoneCount += amount;

        public void ChangeCrouchSafeZoneCount(int amount) => _crouchSafeZoneCount += amount;

        public void ApplyContinuousActionPenalties(bool isJumping, bool isCrouching)
        {
            if (EpisodeController == null) return;

            if (isJumping && EpisodeController.Curriculum.EnableJumpingPenalty && _jumpSafeZoneCount <= 0)
            {
                _agent.AddReward(EpisodeController.Curriculum.JumpingPenalty * Time.deltaTime);
            }

            if (isCrouching && EpisodeController.Curriculum.EnableCrouchingPenalty && _crouchSafeZoneCount <= 0)
            {
                _agent.AddReward(EpisodeController.Curriculum.CrouchingPenalty * Time.deltaTime);
            }
        }
        public void ApplyJumpPenalty()
        {
            if (EpisodeController != null && EpisodeController.Curriculum.EnableJumpingPenalty && _jumpSafeZoneCount <= 0)
            {
                _agent.AddReward(EpisodeController.Curriculum.JumpingPenalty);
            }
        }
        public void ApplyCrouchingPenalty()
        {
            if (EpisodeController != null && EpisodeController.Curriculum.EnableCrouchingPenalty && _crouchSafeZoneCount <= 0)
            {
                _agent.AddReward(EpisodeController.Curriculum.CrouchingPenalty);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (EpisodeController == null) return;
            if (EpisodeController.Curriculum.EnableWallHitPenalty)
            {
                if (hit.gameObject.CompareTag("Obstacle") && Mathf.Abs(hit.normal.y) < 0.2f)
                {
                    _agent.AddReward(EpisodeController.Curriculum.PenaltyForWallHit * Time.deltaTime);
                }
            }
        }
    }
}
