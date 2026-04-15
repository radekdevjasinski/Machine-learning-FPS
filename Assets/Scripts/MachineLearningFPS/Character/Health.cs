using System;
using UnityEngine;
namespace MachineLearningFPS.Character
{
    public class Health : MonoBehaviour
    {
        public float maxHealth = 1f;
        public float CurrentHealth { get; private set; }
        public event Action<GameObject> OnDeath;

        void Start()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            CurrentHealth -= amount;
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"{gameObject.name} has died.");
            OnDeath?.Invoke(gameObject);
            gameObject.SetActive(false);
        }

    }
}

