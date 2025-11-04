using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ReflectableBullet : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 16f;
    public float lifeTime = 6f;

    [Header("Damage / Rules")]
    public int damage = 1;
    public bool requireReflectedToDamageBoss = true;

    [Header("Homing")]
    [Range(0f, 1f)] public float homingStrength = 0.06f;
    public bool retargetContinuously = true;
    public float retargetInterval = 0.2f;

    [Header("VFX")]
    public GameObject impactEffectPrefab;
    public GameObject vanishEffectPrefab;

    [Header("Knockback (Player)")]
    public float knockbackForce = 12f;
    public float knockbackUpward = 3f;

    private Rigidbody _rb;
    private Collider _col;

    private bool _reflected = false;
    private float _dieTime;
    private Collider _ownerToIgnore;
    private float _ignoreUntil = 0f;

    private Transform _bossTarget;
    private float _nextRetargetTime = 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Init(Vector3 initialDir, Collider ownerToIgnore, float ownerIgnoreTime)
    {
        _rb.linearVelocity = initialDir.normalized * speed;
        _ownerToIgnore = ownerToIgnore;

        if (_ownerToIgnore)
        {
            Physics.IgnoreCollision(_ownerToIgnore, _col, true);
            _ignoreUntil = Time.time + Mathf.Max(0f, ownerIgnoreTime);
        }

        _dieTime = Time.time + lifeTime;
    }

    void Update()
    {
        if (Time.time >= _dieTime)
        {
            SpawnVFX(vanishEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        if (_ownerToIgnore && Time.time >= _ignoreUntil)
        {
            Physics.IgnoreCollision(_ownerToIgnore, _col, false);
            _ownerToIgnore = null;
        }
    }

    public void ReflectBy(Vector3 normal)
    {
        Vector3 v = _rb.linearVelocity;
        Vector3 reflected = Vector3.Reflect(v, normal.normalized);
        _rb.linearVelocity = reflected.normalized * speed;
        _reflected = true;

        transform.rotation = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.up);

        _bossTarget = FindNearestBoss();
        _nextRetargetTime = Time.time + retargetInterval;
    }

    void FixedUpdate()
    {
        if (_reflected)
        {
            if (retargetContinuously && Time.time >= _nextRetargetTime)
            {
                _bossTarget = FindNearestBoss();
                _nextRetargetTime = Time.time + retargetInterval;
            }

            if (_bossTarget != null)
            {
                Vector3 desired = (_bossTarget.position - transform.position).normalized;
                Vector3 newVel = Vector3.Lerp(_rb.linearVelocity.normalized, desired, homingStrength) * speed;
                _rb.linearVelocity = newVel;
                transform.rotation = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.up);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Let ReflectWall (if present) own the reflect logic elsewhere.
        if (collision.collider.GetComponentInParent<ReflectWall>() != null)
            return;

        if (collision.collider.CompareTag("Boss"))
        {
            if (!_reflected && requireReflectedToDamageBoss)
            {
                SpawnImpact(collision);
                Destroy(gameObject);
                return;
            }

            var boss = collision.collider.GetComponentInParent<BossShooter>();
            if (boss != null)
            {
                Vector3 hitPoint = SafeContactPoint(collision);
                boss.TakeDamage(damage, hitPoint);
            }

            SpawnImpact(collision);
            Destroy(gameObject);
            return;
        }

        if (collision.collider.CompareTag("Player"))
        {
            var kb =
                collision.collider.GetComponentInParent<KnockbackReceiver>() ??
                collision.collider.GetComponent<KnockbackReceiver>() ??
                collision.collider.GetComponentInChildren<KnockbackReceiver>();

            if (kb != null)
            {
                float upFactor = 0f;
                if (knockbackForce > 0.0001f)
                    upFactor = Mathf.Clamp01(knockbackUpward / knockbackForce);

                kb.AddKnockback(
                    sourcePosition: transform.position,
                    force: knockbackForce,
                    overrideUpFactor: upFactor
                );
            }

            SpawnImpact(collision);
            Destroy(gameObject);
            return;
        }

        SpawnImpact(collision);
        Destroy(gameObject);
    }

    private Transform FindNearestBoss()
    {
        var bosses = GameObject.FindGameObjectsWithTag("Boss");
        Transform nearest = null;
        float bestSqr = float.PositiveInfinity;
        Vector3 p = transform.position;

        for (int i = 0; i < bosses.Length; i++)
        {
            var go = bosses[i];
            if (!go || !go.activeInHierarchy) continue;

            float sqr = (go.transform.position - p).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = go.transform;
            }
        }
        return nearest;
    }

    private void SpawnImpact(Collision c)
    {
        if (!impactEffectPrefab) return;
        Vector3 point = SafeContactPoint(c);
        Vector3 normal = SafeContactNormal(c);
        SpawnVFX(impactEffectPrefab, point, Quaternion.LookRotation(normal));
    }

    private static void SpawnVFX(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!prefab) return;
        var go = Object.Instantiate(prefab, pos, rot);
        Object.Destroy(go, 5f);
    }

    private static Vector3 SafeContactPoint(Collision c)
    {
        if (c.contactCount > 0) return c.GetContact(0).point;
        return c.collider.bounds.ClosestPoint(c.transform.position);
    }

    private static Vector3 SafeContactNormal(Collision c)
    {
        if (c.contactCount > 0) return c.GetContact(0).normal;
        // Fallback normal: from collider center toward bullet
        Vector3 from = c.collider.bounds.center;
        Vector3 to = c.transform.position;
        Vector3 n = (to - from);
        return n.sqrMagnitude > 1e-6f ? n.normalized : Vector3.up;
    }
}