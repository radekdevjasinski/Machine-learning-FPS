using UnityEngine;

namespace MachineLearningFPS.WeaponSystem
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponStats _stats;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private GameObject _lineRendererPrefab;


        public WeaponStats Stats => _stats;
        public Transform FirePoint => _firePoint;
        public GameObject LineRendererPrefab => _lineRendererPrefab;

    }
}