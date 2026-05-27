using UnityEngine;
using Unity.MLAgents;
using System.Collections.Generic;

namespace MachineLearningFPS.Environment
{
    public class TrainingController : MonoBehaviour
    {

        private EpisodeController[] _episodeControllers;
        private List<float> _episodeRewards = new List<float>();
        [SerializeField] private float _logInterval = 30f;
        private float _timer;


        private void Awake()
        {
            _episodeControllers = FindObjectsByType<EpisodeController>();

            if (_episodeControllers.Length == 0)
            {
                Debug.LogWarning("[TrainingController] No EpisodeControllers found in the scene.");
            }
            foreach (var episodeController in _episodeControllers)
            {
                episodeController.OnAgentEpisodeEnd += AddEpisodeReward;
            }
        }
        private void OnDestroy()
        {
            foreach (var episodeController in _episodeControllers)
            {
                episodeController.OnAgentEpisodeEnd -= AddEpisodeReward;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= _logInterval)
            {
                CalculateAndLogRewards();
                _timer = 0f;
            }
        }

        public void AddEpisodeReward(float reward)
        {
            _episodeRewards.Add(reward);
        }

        private void CalculateAndLogRewards()
        {
            if (_episodeRewards.Count == 0) return;

            float totalReward = 0;
            foreach (var reward in _episodeRewards)
            {
                totalReward += reward;
            }
            float averageReward = totalReward / _episodeRewards.Count;

            Debug.Log($"[TrainingController] Average Reward: {averageReward}");

            _episodeRewards.Clear();
        }

    }
}
