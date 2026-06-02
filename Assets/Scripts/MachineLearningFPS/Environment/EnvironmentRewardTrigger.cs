using UnityEngine;
using Unity.MLAgents;
using MachineLearningFPS.Character;

namespace MachineLearningFPS.Environment
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(MeshRenderer))]
    public class EnvironmentRewardTrigger : MonoBehaviour
    {
        [Header("Trigger Constraints")]
        [SerializeField] private string _targetTag = "Player";
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private float _rewardScale = 1f;

        private bool _hasTriggered = false;
        private MeshRenderer _renderer;
        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggerOnce && _hasTriggered) return;

            if (other.CompareTag(_targetTag))
            {
                MLController agent = other.GetComponent<MLController>();

                if (agent != null)
                {
                    agent.ApplyTriggerReward(_rewardScale);
                    if (_triggerOnce)
                    {
                        _hasTriggered = true;
                        _renderer.enabled = false;
                    }
                }
            }
        }


        public void ResetTrigger()
        {
            _hasTriggered = false;
            _renderer.enabled = true;
        }
    }
}
