using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ReflectableBullet : MonoBehaviour
{
    [Header("Bullet")]
    public float speed = 16f;
    public float lifeTime = 6f;
    public int damage = 1;
    public LayerMask portalLayers; // 可选：用于 Layer 识别 Portal，留空则用 Tag

    [Tooltip("反弹后才可以伤害 Boss")]
    public bool requireReflectedToDamageBoss = true;

    private Rigidbody _rb;
    private bool _reflected = false;
    private float _dieTime;
    private Collider _ownerToIgnore;
    private float _ignoreUntil = 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    /// <summary>由 Boss 初始化。</summary>
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
        if (Time.time >= _dieTime) Destroy(gameObject);

        // 忽略超时后恢复与 Boss 的碰撞
        if (_ownerToIgnore && Time.time >= _ignoreUntil)
        {
            Physics.IgnoreCollision(_ownerToIgnore, GetComponent<Collider>(), false);
            _ownerToIgnore = null;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1) 命中 Portal：反弹（使用法线反射）
        if (IsPortal(collision.collider))
        {
            Vector3 v = _rb.linearVelocity;
            Vector3 n = collision.GetContact(0).normal;
            Vector3 reflected = Vector3.Reflect(v, n);
            _rb.linearVelocity = reflected.normalized * speed;
            _reflected = true;
            // 可选：调个角度朝向
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.up);
            return;
        }

        // 2) 命中 Boss：只有被反弹过才造成伤害（根据设置）
        var boss = collision.collider.GetComponentInParent<BossShooter>();
        if (boss != null)
        {
            if (!_reflected && requireReflectedToDamageBoss)
            {
                // 未反弹：不伤害 Boss（可做火花特效）
                Destroy(gameObject);
                return;
            }

            boss.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 3) 命中其他物体：默认销毁（需要可穿透可自定）
        Destroy(gameObject);
    }

    private bool IsPortal(Collider col)
    {
        // 优先 Tag 判断
        if (col.CompareTag("Portal")) return true;

        // 或者 Layer 判断（如设置了 portalLayers）
        if (portalLayers.value != 0)
        {
            int layerMask = 1 << col.gameObject.layer;
            if ((portalLayers.value & layerMask) != 0) return true;
        }
        return false;
    }
}