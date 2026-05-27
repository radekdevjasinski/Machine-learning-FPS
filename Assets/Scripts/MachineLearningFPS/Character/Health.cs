using System;
using UnityEngine;
namespace MachineLearningFPS.Character
{
    public class Health : MonoBehaviour
    {
        public float MaxHealth = 5f;
        public float CurrentHealth { get; private set; }
        public event Action<GameObject, GameObject> OnDeath;
        public static event Action<Transform> OnHealthChanged;
        public event Action<GameObject, GameObject, float> OnDamageTaken;
        private bool _isDead = false;

        void Start()
        {
            CurrentHealth = MaxHealth;
        }

        public void ResetHealth()
        {
            _isDead = false;
            CurrentHealth = MaxHealth;
            //gameObject.SetActive(true);
        }

        public void TakeDamage(float amount, Health attacker)
        {
            if (_isDead) return;
            CurrentHealth -= amount;
            OnHealthChanged?.Invoke(transform);
            OnDamageTaken?.Invoke(gameObject, attacker?.gameObject, amount);
            if (CurrentHealth <= 0)
            {
                Die(attacker);
            }
        }

        private void Die(Health attacker)
        {
            if (_isDead) return;
            _isDead = true;
            OnDeath?.Invoke(gameObject, attacker?.gameObject);
            //gameObject.SetActive(false);
        }

    }
}
