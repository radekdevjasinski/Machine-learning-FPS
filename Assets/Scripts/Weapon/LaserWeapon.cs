using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class LaserWeapon : MonoBehaviour
{
    public Camera playerCamera;
    public InputActionReference fireAction;

    [Header("Blaster Settings")]
    public float range = 100f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 100f;
    public float projectileLifetime = 3f;

    void OnEnable()
    {
        fireAction.action.Enable();
    }

    void OnDisable()
    {
        fireAction.action.Disable();
    }

    void Update()
    {
        if (fireAction.action.triggered)
        {
            ShootLaser();
        }
    }

    void ShootLaser()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            endPoint = hit.point;
            Debug.Log("Trafiono: " + hit.collider.name);
        }
        else
        {
            endPoint = ray.GetPoint(range);
        }

        FireBlaster(endPoint);
    }

    public void FireBlaster(Vector3 targetPoint)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Brak przypisanego prefaba lub firePoint!");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab);

        Vector3 direction = (targetPoint - firePoint.position).normalized;

        projectile.transform.up = direction;

        float offsetDistance = projectile.transform.localScale.y;

        projectile.transform.position = firePoint.position + (direction * offsetDistance);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
        else
        {
            Debug.LogError("Prefab pocisku musi mieć komponent Rigidbody, aby móc się poruszać!");
        }

        Destroy(projectile, projectileLifetime);
    }
}