using UnityEngine;

namespace MachineLearningFPS.WeaponSystem
{
    [CreateAssetMenu(fileName = "WeaponStats", menuName = "WeaponStats", order = 0)]
    public class WeaponStats : ScriptableObject
    {
        [SerializeField] private float damage = 1f;
        [SerializeField] private float range = 100f;
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private int projectileCount = 1;
        [SerializeField] private float spread = 0f;

        public float Damage { get => damage; set => damage = Mathf.Max(0, value); }
        public float Range { get => range; set => range = Mathf.Max(0, value); }
        public float FireRate { get => fireRate; set => fireRate = Mathf.Max(0, value); }
        public int ProjectileCount { get => projectileCount; set => projectileCount = Mathf.Max(1, value); }
        public float Spread { get => spread; set => spread = Mathf.Max(0, value); }
    }
}

