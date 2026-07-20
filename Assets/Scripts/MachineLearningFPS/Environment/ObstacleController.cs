using System.Collections.Generic;
using UnityEngine;

namespace MachineLearningFPS.Environment
{
    public class ObstacleController : MonoBehaviour
    {
        [Header("Obstacle Setup")]
        [SerializeField] private Transform _obstacleParent;
        [SerializeField] private GameObject[] _obstaclePrefabs;

        [Header("Obstacle Movement Bounds")]
        [SerializeField] private float _minX = -10f;
        [SerializeField] private float _maxX = 10f;

        public void ResetObstacles()
        {
            if (!isActiveAndEnabled || this == null)
            {
                return;
            }

            if (_obstacleParent == null || _obstaclePrefabs == null || _obstaclePrefabs.Length == 0)
            {
                Debug.LogWarning("[ObstacleController] Obstacle parent or prefabs not set.");
                return;
            }

            ClearObstacles();
            SpawnObstacles();
        }

        private void ClearObstacles()
        {
            if (_obstacleParent == null)
            {
                return;
            }

            for (int i = _obstacleParent.childCount - 1; i >= 0; i--)
            {
                foreach (Transform child in _obstacleParent.GetChild(i))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void SpawnObstacles()
        {
            if (_obstacleParent == null || _obstaclePrefabs == null || _obstaclePrefabs.Length == 0)
            {
                return;
            }
            int obstacleCount = _obstacleParent.childCount;
            int slotCount = Mathf.Max(1, obstacleCount);
            for (int i = 0; i < slotCount; i++)
            {
                int randomIndex = Random.Range(0, _obstaclePrefabs.Length);
                GameObject newObstacle = Instantiate(_obstaclePrefabs[randomIndex], _obstacleParent.GetChild(i));
                float newX = Random.Range(_minX, _maxX);
                newObstacle.transform.localPosition = new Vector3(newX, newObstacle.transform.localPosition.y, newObstacle.transform.localPosition.z);

            }
        }

    }
}
