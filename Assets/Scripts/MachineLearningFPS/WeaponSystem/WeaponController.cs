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
                RaycastHit hit;
                Vector3 endPoint;

                if (Physics.SphereCast(ray.origin, _laserCastRadius, ray.direction, out hit, stats.Range))
                {
                    endPoint = hit.point;

                    Health targetHealth = hit.collider.GetComponent<Health>();
                    if (targetHealth != null && hit.collider.gameObject != this.gameObject)
                    {
                        Health thisHealth = GetComponentInParent<Health>();
                        if (thisHealth != null)
                        {
                            targetHealth.TakeDamage(stats.Damage, thisHealth);
                        }
                        else
                        {
                            targetHealth.TakeDamage(stats.Damage, null);
                        }
                    }
                }
                else
                {
                    endPoint = ray.GetPoint(stats.Range);
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