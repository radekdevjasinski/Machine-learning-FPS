using System;
using UnityEngine;
namespace MachineLearningFPS.Character
{
    public class Health : MonoBehaviour
    {
        public float maxHealth = 1f;
        public float CurrentHealth { get; private set; }
        public event Action<GameObject, GameObject> OnDeath;

        void Start()
        {
            CurrentHealth = maxHealth;
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            //gameObject.SetActive(true);
        }

        public void TakeDamage(float amount, Health source)
        {
            CurrentHealth -= amount;
            if (CurrentHealth <= 0)
            {
                Die(source);
            }
        }

        private void Die(Health killer)
        {
            Debug.Log($"{gameObject.name} has died.");
            OnDeath?.Invoke(gameObject, killer.gameObject);
            //gameObject.SetActive(false);
        }

    }
}
