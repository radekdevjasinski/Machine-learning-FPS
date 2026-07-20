using System.Collections.Generic;
using UnityEngine;
using MachineLearningFPS.Character;

namespace MachineLearningFPS.Environment
{
    [RequireComponent(typeof(Collider))]
    public class PlayerDamageTriggerZone : MonoBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private string _targetTag = "Player";

        [Header("Damage Settings")]
        [SerializeField] private float _damageAmount = 1f;

        private readonly HashSet<Health> _playersInside = new HashSet<Health>();

        private void Awake()
        {
            var collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_targetTag)) return;
            if (!other.TryGetComponent<Health>(out var health)) return;

            _playersInside.Add(health);

            if (_playersInside.Count == 1)
            {
                var otherPlayer = FindOtherActivePlayer(health);
                if (otherPlayer != null)
                {
                    otherPlayer.TakeDamage(_damageAmount, null);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(_targetTag)) return;
            if (other.TryGetComponent<Health>(out var health))
            {
                _playersInside.Remove(health);
            }
        }

        private Health FindOtherActivePlayer(Health currentPlayer)
        {
            foreach (var health in FindObjectsByType<Health>())
            {
                if (health == null || health == currentPlayer) continue;
                if (!health.gameObject.CompareTag(_targetTag)) continue;
                if (!health.gameObject.activeInHierarchy) continue;
                if (health.CurrentHealth <= 0f) continue;
                if (_playersInside.Contains(health)) continue;

                return health;
            }

            return null;
        }
    }
}
