using NUnit.Framework;
using MachineLearningFPS.MachineLearning;

namespace MachineLearningFPS.Tests.EditMode
{
    public class MLRewardManagerTests
    {
        [Test]
        public void CalculateApproachReward_AtMidRange_ReturnsHalfOfMaxReward()
        {
            float reward = MLRewardManager.CalculateApproachReward(distance: 25f, minDistance: 2f, maxDistance: 50f, rewardScale: 10f);

            Assert.AreEqual(0.005f, reward, 0.0001f);
        }

        [Test]
        public void CalculateApproachReward_DistanceAtOrBelowMinimum_ReturnsZero()
        {
            float reward = MLRewardManager.CalculateApproachReward(distance: 2f, minDistance: 2f, maxDistance: 50f, rewardScale: 10f);

            Assert.AreEqual(0f, reward);
        }

        [Test]
        public void CalculateApproachReward_DistanceAtOrBeyondMaximum_ReturnsZero()
        {
            float reward = MLRewardManager.CalculateApproachReward(distance: 50f, minDistance: 2f, maxDistance: 50f, rewardScale: 10f);

            Assert.AreEqual(0f, reward);
        }

        [Test]
        public void CalculateAimingReward_LookingDirectlyAtTarget_ReturnsFullRewardScale()
        {
            float reward = MLRewardManager.CalculateAimingReward(aimQualityDot: 1f, coneAngleDegrees: 30f, rewardScale: 2f);

            Assert.AreEqual(2f, reward, 0.0001f);
        }

        [Test]
        public void CalculateAimingReward_AtConeEdge_ReturnsZero()
        {
            float coneEdgeDot = UnityEngine.Mathf.Cos(30f * UnityEngine.Mathf.Deg2Rad);

            float reward = MLRewardManager.CalculateAimingReward(aimQualityDot: coneEdgeDot, coneAngleDegrees: 30f, rewardScale: 2f);

            Assert.AreEqual(0f, reward, 0.0001f);
        }

        [Test]
        public void CalculateAimingReward_HalfwayThroughCone_ReturnsHalfRewardScale()
        {
            float reward = MLRewardManager.CalculateAimingReward(aimQualityDot: 0.5f, coneAngleDegrees: 90f, rewardScale: 4f);

            Assert.AreEqual(2f, reward, 0.0001f);
        }
    }
}
