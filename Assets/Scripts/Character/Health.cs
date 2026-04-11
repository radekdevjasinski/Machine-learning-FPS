using System;
using UnityEngine;
public class Health : MonoBehaviour
{
    public float maxHealth = 1f;
    private float currentHealth;
    public event Action<GameObject> OnDeath;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
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