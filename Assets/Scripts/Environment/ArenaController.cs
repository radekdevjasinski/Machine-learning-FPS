using System;
using System.Collections.Generic;
using UnityEngine;
using MachineLearningFPS.Character;

namespace MachineLearningFPS.Environment
{
    public class ArenaController : MonoBehaviour
    {
        [Header("Arena Settings")]
        [SerializeField] private Transform _spawnPointsParent;
        [SerializeField] private Transform _movingObstaclesParent;
        [SerializeField] private bool _applyStartingRotationRandomization = true;
        [SerializeField] private bool _usePredifinedSpawnPoints = true;
        [SerializeField] private bool _obstacleMovementEnabled = true;
        [SerializeField] private bool _agentsSpawnOffsetEnabled = true;



        [Serializable]
        private struct LevelBounds
        {
            public float MinX, MaxX, MinZ, MaxZ;
        }
        [SerializeField] private LevelBounds _levelBounds;

        private List<Transform> _spawnPoints;

        private void Awake()
        {
            _spawnPoints = new List<Transform>();
            if (_spawnPointsParent != null)
            {
                foreach (Transform child in _spawnPointsParent)
                {
                    _spawnPoints.Add(child);
                }
            }
        }

        public void ResetArena(List<MLController> agents)
        {
            if (_usePredifinedSpawnPoints)
            {
                PreidentifiedSpawnReset(agents);
            }
            else
            {
                RandomSpawnReset(agents);
            }
            if (_obstacleMovementEnabled)
            {
                MoveArenaObstacles();
            }
        }

        private void PreidentifiedSpawnReset(List<MLController> agents)
        {
            List<Transform> availableSpawns = new(_spawnPoints);
            foreach (var agent in agents)
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
                if (_agentsSpawnOffsetEnabled)
                {
                    MoveAgents(agent.transform);
                }
            }
        }

        private void RandomSpawnReset(List<MLController> agents)
        {
            const int MAX_SPAWN_ATTEMPTS = 30;
            const float MIN_SPAWN_DISTANCE = 1.0f;
            List<Vector3> usedPositions = new List<Vector3>();

            foreach (var agent in agents)
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
                    Debug.LogWarning($"[ArenaController] Could not find a clear spawn point for {agent.gameObject.name} after {MAX_SPAWN_ATTEMPTS} attempts.");
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
        private void MoveArenaObstacles()
        {
            if (_movingObstaclesParent == null) return;

            foreach (Transform obstacle in _movingObstaclesParent)
            {
                float newX = UnityEngine.Random.Range(_levelBounds.MinX, _levelBounds.MaxX);
                obstacle.localPosition = new Vector3(newX, obstacle.position.y, obstacle.position.z);
            }
        }
        private void MoveAgents(Transform agent)
        {
            if (agent == null) return;
            float newX = UnityEngine.Random.Range(_levelBounds.MinX, _levelBounds.MaxX);
            agent.localPosition = new Vector3(newX, agent.position.y, agent.position.z);
        }
    }
}