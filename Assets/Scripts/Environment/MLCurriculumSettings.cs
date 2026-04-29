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

        [Header("Movement Rewards")]
        public bool EnableWallHitPenalty = false;
        public float PenaltyForWallHit = -0.25f;

        [Header("Continuous Rewards")]
        public bool EnableExistancePenalty = false;
        public float ExistancePenaltyAmount = 0.01f;

        [Header("Episode Rewards")]
        public bool EnableTrucePenalty = false;
        public float TrucePenaltyAmount = -10f;
        public float KillRewardAmount = 10f;


    }
}