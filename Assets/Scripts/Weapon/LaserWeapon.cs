using System.Collections;
using UnityEngine;

public class LaserWeapon : MonoBehaviour
{

    [Header("Gameplay")]
    [SerializeField] private float _damage = 1f;
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _fireRate = 0.5f;
    private float _lastFireTime;

    [Header("Visuals")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private float _laserDuration = 0.05f;

    private void Awake()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
        else
        {
            Debug.LogError("Brak przypisanego LineRenderera w skrypcie LaserWeapon!");
        }
    }

    public void Shoot()
    {
        if (Time.time - _lastFireTime < _fireRate) return;

        _lastFireTime = Time.time;
        ShootLaser();
    }

    public void ShootLaser()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(ray, out hit, _range))
        {
            endPoint = hit.point;
            Debug.Log("Trafiono: " + hit.collider.name);

            Health targetHealth = hit.collider.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(_damage);
            }
        }
        else
        {
            endPoint = ray.GetPoint(_range);
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