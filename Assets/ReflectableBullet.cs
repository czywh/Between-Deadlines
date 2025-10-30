using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ReflectableBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 16f;
    public float lifeTime = 6f;
    public int damage = 1;
    [Tooltip("只有被反弹过才会伤害 Boss")]
    public bool requireReflectedToDamageBoss = true;

    [Header("Homing")]
    [Range(0f, 1f)] public float homingStrength = 0.06f;

    [Tooltip("子弹被任何东西销毁/命中时的通用粒子（可空）")]
    public GameObject impactEffectPrefab;
    [Tooltip("子弹寿命到期时播放的消散特效（可空）")]
    public GameObject vanishEffectPrefab;

    [Header("Player Knockback")]
    [Tooltip("击中玩家时的击退力度")]
    public float knockbackForce = 12f;
    [Tooltip("击中玩家时附加的向上力度")]
    public float knockbackUpward = 3f;

    private Rigidbody _rb;
    private bool _reflected = false;
    private float _dieTime;
    private Collider _ownerToIgnore;
    private float _ignoreUntil = 0f;

    private Transform _bossTarget;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Init(Vector3 initialDir, Collider ownerToIgnore, float ownerIgnoreTime)
    {
        _rb.linearVelocity = initialDir.normalized * speed;
        _ownerToIgnore = ownerToIgnore;
        if (_ownerToIgnore)
        {
            Physics.IgnoreCollision(_ownerToIgnore, GetComponent<Collider>(), true);
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
            Physics.IgnoreCollision(_ownerToIgnore, GetComponent<Collider>(), false);
            _ownerToIgnore = null;
        }
    }

    /// 由 ReflectWall 调用：按法线反弹，并开始追踪 Boss（Tag）
    public void ReflectBy(Vector3 normal)
    {
        Vector3 v = _rb.linearVelocity;
        Vector3 reflected = Vector3.Reflect(v, normal.normalized);
        _rb.linearVelocity = reflected.normalized * speed;
        _reflected = true;
        transform.rotation = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.up);

        var bossObj = GameObject.FindGameObjectWithTag("Boss");
        _bossTarget = bossObj ? bossObj.transform : null;
    }

    void FixedUpdate()
    {
        if (_reflected && _bossTarget != null)
        {
            Vector3 desired = (_bossTarget.position - transform.position).normalized;
            Vector3 newVel = Vector3.Lerp(_rb.linearVelocity.normalized, desired, homingStrength) * speed;
            _rb.linearVelocity = newVel;
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.up);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 让墙体自己处理反弹（它会调用 ReflectBy）
        if (collision.collider.GetComponentInParent<ReflectWall>() != null)
            return;

        // ① 撞到 Boss
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
                Vector3 hitPoint = collision.GetContact(0).point;
                boss.TakeDamage(damage, hitPoint);
            }
            SpawnImpact(collision);
            Destroy(gameObject);
            return;
        }

        // === ② 撞到 Player：施加击退 ===
        if (collision.collider.CompareTag("Player"))
        {
            
            var kb = collision.collider.GetComponentInParent<KnockbackReceiver>() ??
                     collision.collider.GetComponent<KnockbackReceiver>() ??
                     collision.collider.GetComponentInChildren<KnockbackReceiver>();

            if (kb != null)
            {
                float upFactor = 0f;
                if (knockbackForce > 0.0001f)
                    upFactor = Mathf.Clamp01(knockbackUpward / knockbackForce);

                
                kb.AddKnockback(sourcePosition: transform.position,
                                force: knockbackForce,
                                overrideUpFactor: upFactor);
            }

            
            SpawnImpact(collision);
            Destroy(gameObject);
            return;
        }


        // ③ 其他物体 → 默认播放通用击中 VFX 后销毁
        SpawnImpact(collision);
        Destroy(gameObject);
    }

    //—— 工具 ——//
    private void SpawnImpact(Collision c)
    {
        if (!impactEffectPrefab) return;
        var cp = c.GetContact(0);
        SpawnVFX(impactEffectPrefab, cp.point, Quaternion.LookRotation(cp.normal));
    }

    private static void SpawnVFX(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!prefab) return;
        var go = Object.Instantiate(prefab, pos, rot);
        // 可选：给特效加自毁
        Object.Destroy(go, 5f);
    }
}