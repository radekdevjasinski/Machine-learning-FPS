using System.Collections.Generic;
using UnityEngine;
using MachineLearningFPS.Character;
using System;

namespace MachineLearningFPS.Environment
{
    public class EpisodeController : MonoBehaviour
    {
        [Header("Environment Settings")]
        [SerializeField] private List<MLController> _agents;
        [SerializeField] private Transform _spawnPointsParent;
        [SerializeField] private float _maxEpisodeLength = 60f;
        [SerializeField] private bool _applyStartingRotationRandomization = true;
        [SerializeField] private bool _usePredifinedSpawnPoints = true;
        [Serializable]
        private struct LevelBounds
        {
            public float MinX, MaxX, MinZ, MaxZ;
        }
        [SerializeField] private LevelBounds _levelBounds;

        [Header("Curriculum Settings")]
        [SerializeField] private MLCurriculumSettings _curriculum;
        public MLCurriculumSettings Curriculum => _curriculum;
        private List<Transform> _spawnPoints;
        private float _currentEpisodeTime = 0f;
        private bool _timeUp = false;

        public static event Action<int> OnPlayerKilled;
        public static event Action OnEpisodeReset;

        private void Start()
        {
            foreach (var agent in _agents)
            {
                if (agent.TryGetComponent(out Health health))
                {
                    health.OnDeath += HandleAgentDeath;
                }
            }
            _spawnPoints = new List<Transform>();
            foreach (Transform child in _spawnPointsParent)
            {
                _spawnPoints.Add(child);
            }

            ResetEnvironment();
        }

        private void OnDestroy()
        {
            foreach (var agent in _agents)
            {
                if (agent != null)
                {
                    if (agent.TryGetComponent(out Health health))
                    {
                        health.OnDeath -= HandleAgentDeath;
                    }
                }
            }
        }

        private void Update()
        {
            _currentEpisodeTime += Time.deltaTime;

            if (_currentEpisodeTime >= _maxEpisodeLength)
            {
                _timeUp = true;
                ResetEpisode();
            }
        }


        private void HandleAgentDeath(GameObject victim, GameObject killer)
        {
            if (killer != null && killer.TryGetComponent(out MLController killerML))
            {
                killerML.ApplyKillReward();
                int killerTeam = killerML.TeamID;
                OnPlayerKilled?.Invoke(killerTeam);
            }

            if (victim != null && victim.TryGetComponent(out MLController victimML))
            {
                victimML.ApplyDeathPenalty();
            }

            ResetEpisode();
        }

        private void ResetEpisode()
        {
            _currentEpisodeTime = 0f;

            foreach (var agent in _agents)
            {
                if (_timeUp && _curriculum.EnableTrucePenalty)
                {
                    agent.AddReward(_curriculum.TrucePenaltyAmount);
                }
                agent.EndEpisode();
            }

            _timeUp = false;
            ResetEnvironment();
            OnEpisodeReset?.Invoke();
        }

        private void ResetEnvironment()
        {
            foreach (var agent in _agents)
            {
                if (agent.TryGetComponent(out Health health)) health.ResetHealth();
                if (agent.TryGetComponent(out FPSMovement movement)) movement.ResetMovement();
            }
            if (_usePredifinedSpawnPoints)
            {
                PreidentifiedSpawnReset();
            }
            else
            {
                RandomSpawnReset();
            }
        }
        private void PreidentifiedSpawnReset()
        {
            List<Transform> availableSpawns = new(_spawnPoints);
            foreach (var agent in _agents)
            {
                if (availableSpawns.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, availableSpawns.Count);
                    Transform spawn = availableSpawns[randomIndex];
                    availableSpawns.RemoveAt(randomIndex);

                    if (agent.TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;
                    agent.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
                    if (cc != null) cc.enabled = true;
                }
                if (_applyStartingRotationRandomization)
                {
                    agent.transform.rotation = Quaternion.Euler(0, agent.transform.rotation.eulerAngles.y + UnityEngine.Random.Range(-45f, 45f), 0);
                }
            }
        }
        private void RandomSpawnReset()
        {
            const int MAX_SPAWN_ATTEMPTS = 30;
            const float MIN_SPAWN_DISTANCE = 1.0f;
            List<Vector3> usedPositions = new List<Vector3>();

            foreach (var agent in _agents)
            {
                Vector3 worldSpawnPosition = transform.position;
                bool foundValidPosition = false;

                for (int i = 0; i < MAX_SPAWN_ATTEMPTS; i++)
                {
                    Vector3 localOffset = new Vector3(
                        UnityEngine.Random.Range(_levelBounds.MinX, _levelBounds.MaxX),
                        1f,
                        UnityEngine.Random.Range(_levelBounds.MinZ, _levelBounds.MaxZ)
                    );

                    Vector3 potentialPosition = transform.TransformPoint(localOffset);

                    bool isPositionClear = true;
                    foreach (var usedPos in usedPositions)
                    {
                        if (Vector3.Distance(potentialPosition, usedPos) < MIN_SPAWN_DISTANCE)
                        {
                            isPositionClear = false;
                            break;
                        }
                    }

                    if (isPositionClear)
                    {
                        worldSpawnPosition = potentialPosition;
                        usedPositions.Add(worldSpawnPosition);
                        foundValidPosition = true;
                        break;
                    }
                }

                if (!foundValidPosition)
                {
                    Debug.LogWarning($"[EpisodeController] Could not find a clear spawn point for {agent.gameObject.name} after {MAX_SPAWN_ATTEMPTS} attempts.");
                }

                if (agent.TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;
                agent.transform.position = worldSpawnPosition;
                if (cc != null) cc.enabled = true;

                if (_applyStartingRotationRandomization)
                {
                    agent.transform.rotation = transform.rotation * Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
                }
            }
        }
    }
}
