using System;
using UnityEngine;

namespace MachineLearningFPS.Environment
{
    [CreateAssetMenu(fileName = "NewCurriculumSettings", menuName = "ML-Agents/Curriculum Settings")]
    public class MLCurriculumSettings : ScriptableObject
    {
        [Header("World Interactions")]
        public bool EnableJumping = false;
        public bool EnableCrouching = false;
        public bool EnableVerticalLooking = false;
        public bool EnableWeaponSwapping = false;

        [Header("Reward Settings")]
        [Header("Aiming")]
        public bool EnableFirstSightReward = false;
        public float RewardForFirstSight = 2f;
        public bool EnableLookingAtEnemyReward = false;
        public float RewardForLookingAtEnemy = 0.08f;
        public bool EnableGoodShootReward = false;
        public float RewardForGoodShoot = 0.5f;
        public bool EnableBadShootPenalty = false;
        public float PenaltyForBadShoot = -0.1f;
        public float AimingConeAngle = 5f;
        public bool EnableAimingQualityReward = false;
        public float AimingQualityRewardScale = 0.15f;

        [Header("Movement Rewards (per second)")]
        public bool EnableWallHitPenalty = false;
        public float PenaltyForWallHit = -0.25f;

        [Header("Actions Penalties (per second)")]
        [Tooltip("Enable a penalty when the agent performs an action. (Blue trigger zone disable this penalty)")]
        public bool EnableJumpingPenalty = false;
        public float JumpingPenalty = -1f;
        public bool EnableCrouchingPenalty = false;
        public float CrouchingPenalty = -1f;


        [Header("Continuous Rewards (per second)")]
        public bool EnableExistancePenalty = false;
        public float ExistancePenaltyAmount = -0.1f;

        [Header("Episode Rewards")]
        [Tooltip("Truce and death penalties.")]
        public bool EnableTrucePenalty = false;
        public float TrucePenaltyAmount = -10f;
        public bool EnableKillReward = true;
        public float KillRewardAmount = 20f;
        public bool EnableDeathPenalty = false;
        public float DeathPenaltyAmount = -8f;
        public bool EnableWonByZoneReward = false;
        public float WonByZoneRewardAmount = 10f;

        [Header("Damage Settings")]
        [Tooltip("Enable a reward when the agent deals damage to the enemy.")]
        public float DealingDamageRewardScale = 5f;
        public float TakingDamagePenaltyScale = 2f;

        [Header("Contact Settings")]
        [Tooltip("Enable a penalty when the agent comes into contact with the enemy.")]
        public bool EnableContactPenalty = false;
        public float ContactPenaltyAmount = -0.15f;
        public float ContactDetectionRadius = 2f;

        [Header("Approach Rewards (per second)")]
        [Tooltip("Enable a reward when the agent approaches the enemy.")]
        public bool EnableApproachReward = false;
        public float ApproachRewardScale = 20f;
        public float ApproachRewardMaxDistance = 20f;
        public float ApproachRewardMinDistance = 3f;

        [Header("Weapon Swap Penalty")]
        [Tooltip("Enable a penalty each time the agent switches weapon, to discourage twitchy swapping.")]
        public bool EnableWeaponChangePenalty = false;
        public float WeaponChangePenaltyAmount = -0.05f;

        [Header("Trigger Rewards")]
        [Tooltip("Enable a reward when the agent enters the green trigger collider. They are placed behind jump and crouch obstacles.")]
        public bool EnableTriggerReward = false;
        public float TriggerRewardAmount = 10f;
        public bool EnableKingOfTheHillReward = false;
        public float KingOfTheHillRewardAmount = 0.05f;

        [Header("Battle Royale Zone")]
        [Tooltip("Shrinking zone that pushes agents toward the arena center. Deals damage (or a reward penalty) to agents caught outside the current radius.")]
        public bool EnableBattleRoyaleZone = false;
        public bool BattleRoyaleDamageEnabled = false;
        [Tooltip("Shrink duration as a percentage of the episode's total length (_maxEpisodeSteps), so it scales automatically with episode length instead of a fixed seconds value.")]
        public float ShrinkDurationInEpisodePercent = 50f;
        public float BattleRoyalePenaltyAmount = 1f;

    }
}