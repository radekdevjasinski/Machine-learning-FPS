using System.Collections.Generic;
using UnityEngine;
using MachineLearningFPS.Character;

namespace MachineLearningFPS.Environment
{
    public class BattleRoyaleZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private float _maxRadius = 20f;
        [SerializeField] private float _minRadius = 2f;
        [SerializeField] private float _shrinkDuration = 25f;
        [SerializeField] private float _damagePerSecond = 1f;
        [SerializeField] private bool _damageEnabled = false;
        public bool DamageEnabled
        {
            get => _damageEnabled;
            set => _damageEnabled = value;
        }

        [Header("References")]
        [SerializeField] private Transform _visualCylinder;

        private float _currentRadius;
        private bool _isActive = false;
        [SerializeField] private List<Health> _agentHealths;

        public void InitializeZone(List<Health> _agentList, float damagePerSecond)
        {
            _damagePerSecond = damagePerSecond;
            _agentHealths = _agentList;
            ResetZone();
        }
        public void ResetZone()
        {
            _currentRadius = _maxRadius;
            _isActive = true;
            UpdateVisualCylinder();
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;

            if (_currentRadius > _minRadius)
            {
                float shrinkSpeed = (_maxRadius - _minRadius) / _shrinkDuration;
                _currentRadius -= shrinkSpeed * Time.fixedDeltaTime;
                UpdateVisualCylinder();
            }

            if (_damageEnabled)
            {
                foreach (var health in _agentHealths)
                {
                    if (health != null && health.CurrentHealth > 0)
                    {
                        Vector3 centerPos = new Vector3(transform.position.x, 0, transform.position.z);
                        Vector3 agentPos = new Vector3(health.transform.position.x, 0, health.transform.position.z);

                        float distanceToCenter = Vector3.Distance(centerPos, agentPos);

                        if (distanceToCenter > _currentRadius)
                        {
                            health.TakeDamage(_damagePerSecond * Time.fixedDeltaTime, null);
                        }
                    }
                }
            }
            else
            {
                foreach (var health in _agentHealths)
                {
                    Vector3 centerPos = new Vector3(transform.position.x, 0, transform.position.z);
                    Vector3 agentPos = new Vector3(health.transform.position.x, 0, health.transform.position.z);

                    float distanceToCenter = Vector3.Distance(centerPos, agentPos);
                    if (distanceToCenter > _currentRadius)
                    {
                        if (health.TryGetComponent<MLController>(out var mlController))
                        {
                            mlController.AddReward(-_damagePerSecond * Time.fixedDeltaTime);
                        }
                    }

                }
            }

        }

        private void UpdateVisualCylinder()
        {
            if (_visualCylinder != null)
            {
                float scale = _currentRadius * 2f;
                _visualCylinder.localScale = new Vector3(scale, _visualCylinder.localScale.y, scale);
            }
        }
    }
}