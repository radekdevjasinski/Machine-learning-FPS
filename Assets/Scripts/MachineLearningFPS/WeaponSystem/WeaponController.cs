using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MachineLearningFPS.Character;

namespace MachineLearningFPS.WeaponSystem
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private List<Weapon> weapons = new List<Weapon>();
        [SerializeField] private int startingWeaponIndex = 0;
        [SerializeField] private float _laserDuration = 0.05f;
        [SerializeField] private float _fadeDuration = 0.01f;
        [SerializeField] private float _laserCastRadius = 0.25f;
        public int WeaponCount => weapons.Count;

        private Weapon _currentWeapon;
        private int _currentWeaponIndex = -1;
        public int CurrentWeaponIndex => _currentWeaponIndex;
        public float CurrentWeaponRange => _currentWeapon != null && _currentWeapon.Stats != null ? _currentWeapon.Stats.Range : 0f;
        private float _lastFireTime;
        private Transform _aimTransform;
        private RaycastHit[] _hitBuffer = new RaycastHit[32];

        private void Awake()
        {
            foreach (var weapon in GetComponentsInChildren<Weapon>(true))
            {
                if (!weapons.Contains(weapon))
                {
                    weapons.Add(weapon);
                }
                weapon.gameObject.SetActive(false);
            }

            if (weapons.Count > 0)
            {
                EquipWeapon(startingWeaponIndex);
            }
        }

        public void SetAimTransform(Transform aimTransform)
        {
            _aimTransform = aimTransform;
        }

        public void EquipWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count || index == _currentWeaponIndex)
            {
                return;
            }

            if (_currentWeapon != null)
            {
                _currentWeapon.gameObject.SetActive(false);
            }

            _currentWeaponIndex = index;
            _currentWeapon = weapons[_currentWeaponIndex];
            _currentWeapon.gameObject.SetActive(true);
            _lastFireTime = Time.time;
        }

        public bool CanShoot()
        {
            if (_currentWeapon == null || _currentWeapon.Stats == null) return false;
            return Time.time - _lastFireTime >= _currentWeapon.Stats.FireRate;
        }

        public float ShootReadinessPercentage
        {
            get
            {
                if (_currentWeapon == null || _currentWeapon.Stats == null) return 0f;
                float timeSinceLastShot = Time.time - _lastFireTime;
                return Mathf.Clamp01(timeSinceLastShot / _currentWeapon.Stats.FireRate);
            }
        }

        public bool Shoot()
        {
            if (!CanShoot() || _currentWeapon == null) return false;

            WeaponStats currentStats = _currentWeapon.Stats;
            _lastFireTime = Time.time;

            if (_aimTransform == null)
            {
                var fpsMovement = GetComponentInParent<FPSMovement>();
                if (fpsMovement != null) _aimTransform = fpsMovement.HeadTransform;
                else return false;
            }

            ShootWeapon(currentStats);
            return true;
        }

        private void ShootWeapon(WeaponStats stats)
        {
            Health thisHealth = GetComponentInParent<Health>();

            for (int i = 0; i < stats.ProjectileCount; i++)
            {
                Vector3 direction = _aimTransform.forward;

                if (stats.Spread > 0)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * stats.Spread;
                    Quaternion spreadRotation = Quaternion.Euler(randomCircle.y, randomCircle.x, 0);
                    direction = spreadRotation * direction;
                }

                Ray ray = new Ray(_aimTransform.position, direction);
                Vector3 endPoint = ray.GetPoint(stats.Range);

                int hitCount = Physics.SphereCastNonAlloc(ray.origin, _laserCastRadius, ray.direction, _hitBuffer, stats.Range);

                if (hitCount > 0)
                {
                    float closestDistance = float.MaxValue;
                    bool hitValidTarget = false;
                    RaycastHit closestHit = default;
                    Health closestHealth = null;

                    for (int j = 0; j < hitCount; j++)
                    {
                        RaycastHit hit = _hitBuffer[j];
                        Health targetHealth = hit.collider.GetComponentInParent<Health>();

                        if (targetHealth != null && targetHealth == thisHealth) continue;

                        if (hit.distance < closestDistance)
                        {
                            closestDistance = hit.distance;
                            closestHit = hit;
                            closestHealth = targetHealth;
                            hitValidTarget = true;
                        }
                    }

                    if (hitValidTarget)
                    {
                        // If the sphere starts overlapping a collider, distance is 0 and point is Vector3.zero
                        endPoint = closestHit.distance == 0f ? ray.origin : closestHit.point;
                        if (closestHealth != null)
                        {
                            closestHealth.TakeDamage(stats.Damage, thisHealth);
                        }
                    }
                }

                StartCoroutine(RenderTraceCoroutine(endPoint, _currentWeapon.LineRendererPrefab));
            }
        }


        private IEnumerator RenderTraceCoroutine(Vector3 targetPoint, GameObject lineRendererPrefab)
        {
            if (lineRendererPrefab == null)
            {
                Debug.LogWarning("LineRenderer prefab not set on WeaponController. Cannot render shot trace.");
                yield break;
            }
            GameObject gameObject = Instantiate(lineRendererPrefab);
            LineRenderer lr = gameObject.GetComponent<LineRenderer>();

            if (lr == null)
            {
                Debug.LogWarning("LineRenderer component missing on prefab. Cannot render shot trace.");
                Destroy(gameObject);
                yield break;
            }

            float laserThickness = _laserCastRadius;
            lr.startWidth = laserThickness;
            lr.endWidth = laserThickness / 2f;

            Transform firePoint = _currentWeapon.FirePoint;
            lr.transform.position = firePoint.position;
            lr.SetPosition(0, firePoint.position);
            lr.SetPosition(1, targetPoint);
            lr.enabled = true;

            yield return new WaitForSeconds(_laserDuration);
            // Fade out
            float elapsed = 0f;
            Color originalStartColor = lr.startColor;
            Color originalEndColor = lr.endColor;
            Color transparentStartColor = originalStartColor;
            Color transparentEndColor = originalEndColor;
            transparentStartColor.a = 0f;
            transparentEndColor.a = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                lr.startColor = Color.Lerp(originalStartColor, transparentStartColor, t);
                lr.endColor = Color.Lerp(originalEndColor, transparentEndColor, t);
                lr.SetPosition(0, firePoint.position);
                yield return null;
            }

            lr.enabled = false;
            Destroy(lr.gameObject);
        }
    }
}