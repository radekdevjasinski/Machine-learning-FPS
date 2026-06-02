using UnityEngine;
using MachineLearningFPS.Character;

namespace MachineLearningFPS.Environment
{
    [RequireComponent(typeof(Collider))]
    public class StanceSafeZoneTrigger : MonoBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private string _targetTag = "Player";

        [Header("Safe Zone Settings")]
        [SerializeField] private bool _allowsFreeJumping = true;
        [SerializeField] private bool _allowsFreeCrouching = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_targetTag) && other.TryGetComponent<MLRewardManager>(out var rewardManager))
            {
                if (_allowsFreeJumping) rewardManager.ChangeJumpSafeZoneCount(1);
                if (_allowsFreeCrouching) rewardManager.ChangeCrouchSafeZoneCount(1);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_targetTag) && other.TryGetComponent<MLRewardManager>(out var rewardManager))
            {
                if (_allowsFreeJumping) rewardManager.ChangeJumpSafeZoneCount(-1);
                if (_allowsFreeCrouching) rewardManager.ChangeCrouchSafeZoneCount(-1);
            }
        }
    }
}
