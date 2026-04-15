using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Definitions")]
    [SerializeField] private WeaponStats laserStats;
    [SerializeField] private WeaponStats sniperStats;
    [SerializeField] private WeaponStats shotgunStats;

    [Header("Visuals")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private float _laserDuration = 0.05f;

    private float _lastFireTime;
    private Transform _aimTransform;
    private Dictionary<WeaponType, WeaponStats> _weaponStatsDict;
    private WeaponType _currentWeaponType = WeaponType.Laser;

    private void Awake()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
        _weaponStatsDict = new Dictionary<WeaponType, WeaponStats>
        {
            { WeaponType.Laser, laserStats },
            { WeaponType.Sniper, sniperStats },
            { WeaponType.Shotgun, shotgunStats }
        };
    }

    public void SetAimTransform(Transform aimTransform)
    {
        _aimTransform = aimTransform;
    }

    public void SwitchWeapon(WeaponType newWeapon)
    {
        _currentWeaponType = newWeapon;
    }

    public bool CanShoot()
    {
        if (_weaponStatsDict == null || !_weaponStatsDict.ContainsKey(_currentWeaponType)) return false;
        return Time.time - _lastFireTime >= _weaponStatsDict[_currentWeaponType].fireRate;
    }

    public void Shoot()
    {
        if (!CanShoot()) return;

        WeaponStats currentStats = _weaponStatsDict[_currentWeaponType];
        _lastFireTime = Time.time;

        switch (_currentWeaponType)
        {
            case WeaponType.Laser:
            case WeaponType.Sniper:
                ShootHitscan(currentStats);
                break;
            case WeaponType.Shotgun:
                ShootShotgun(currentStats);
                break;
        }
    }

    /// <summary>
    /// Fires a single, accurate raycast. Used for Laser and Sniper.
    /// </summary>
    private void ShootHitscan(WeaponStats stats)
    {
        if (_aimTransform == null)
        {
            Debug.LogWarning("Aim Transform not set on LaserWeapon, falling back to Camera.main. This is required for ML-Agents.");
            _aimTransform = Camera.main.transform;
            if (_aimTransform == null) return;
        }

        Ray ray = new Ray(_aimTransform.position, _aimTransform.forward);
        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(ray, out hit, stats.range))
        {
            endPoint = hit.point;
            Debug.Log("Trafiono: " + hit.collider.name);

            Health targetHealth = hit.collider.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(stats.damage);
            }
        }
        else
        {
            endPoint = ray.GetPoint(stats.range);
        }

        StartCoroutine(RenderLaserCoroutine(endPoint));
    }

    /// <summary>
    /// Fires multiple raycasts in a cone. Used for the Shotgun.
    /// </summary>
    private void ShootShotgun(WeaponStats stats)
    {
        if (_aimTransform == null)
        {
            Debug.LogWarning("Aim Transform not set on LaserWeapon, cannot shoot.");
            return;
        }

        for (int i = 0; i < stats.projectileCount; i++)
        {
            // Get a random point in a circle to define the spread direction
            Vector2 randomCircle = Random.insideUnitCircle * stats.spreadAngle;
            Quaternion spreadRotation = Quaternion.Euler(randomCircle.y, randomCircle.x, 0);

            // Apply the spread to the aiming direction
            Vector3 direction = spreadRotation * _aimTransform.forward;

            Ray ray = new Ray(_aimTransform.position, direction);
            RaycastHit hit;
            Vector3 endPoint;

            if (Physics.Raycast(ray, out hit, stats.range))
            {
                endPoint = hit.point;

                Health targetHealth = hit.collider.GetComponent<Health>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(stats.damage); // Damage per pellet
                }
            }
            else
            {
                endPoint = ray.GetPoint(stats.range);
            }

            StartCoroutine(RenderLaserCoroutine(endPoint));
        }

        StartCoroutine(RenderLaserCoroutine(endPoint));
    }

    private IEnumerator RenderLaserCoroutine(Vector3 targetPoint)
    {
        if (_lineRenderer == null || _firePoint == null) yield break;

        _lineRenderer.SetPosition(0, _firePoint.position);
        _lineRenderer.SetPosition(1, targetPoint);

        _lineRenderer.enabled = true;

        yield return new WaitForSeconds(_laserDuration);

        _lineRenderer.enabled = false;
    }
}