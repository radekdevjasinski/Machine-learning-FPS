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
        [SerializeField] private float _fadeDuration = 0.5f;
        public int WeaponCount => weapons.Count;

        private Weapon _currentWeapon;
        private int _currentWeaponIndex = -1;
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
        }

        public bool CanShoot()
        {
            if (_currentWeapon == null || _currentWeapon.Stats == null) return false;
            return Time.time - _lastFireTime >= _currentWeapon.Stats.FireRate;
        }

        public void Shoot()
        {
            if (!CanShoot() || _currentWeapon == null) return;

            WeaponStats currentStats = _currentWeapon.Stats;
            _lastFireTime = Time.time;

            if (_aimTransform == null)
            {
                Debug.LogWarning("Aim Transform not set on WeaponController. This is required for ML-Agents.");
                var fpsMovement = GetComponentInParent<FPSMovement>();
                if (fpsMovement != null) _aimTransform = fpsMovement.HeadTransform;
                else return;
            }

            ShootWeapon(currentStats);
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

                if (Physics.Raycast(ray, out hit, stats.Range))
                {
                    endPoint = hit.point;

                    Health targetHealth = hit.collider.GetComponent<Health>();
                    if (targetHealth != null)
                    {
                        targetHealth.TakeDamage(stats.Damage);
                    }
                }
                else
                {
                    endPoint = ray.GetPoint(stats.Range);
                }

                StartCoroutine(RenderTraceCoroutine(endPoint, _currentWeapon.LineRendererPrefab));
            }
        }


        // TODO: This method doesn't make the laser fade away properly
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

            lr.transform.position = _currentWeapon.FirePoint.position;
            lr.SetPosition(0, _currentWeapon.FirePoint.position);
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
                yield return null;
            }

            lr.enabled = false;
            Destroy(lr.gameObject);
        }
    }
}