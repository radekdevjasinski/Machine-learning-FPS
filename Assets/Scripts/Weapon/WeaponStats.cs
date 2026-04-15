[System.Serializable]
public class WeaponStats
{
    public string name;
    public float damage = 1f;
    public float range = 100f;
    public float fireRate = 0.5f;
    [Header("Shotgun Specific")]
    public int projectileCount = 8;
    public float spreadAngle = 15f;
}
