using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class BossShooter : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxHP = 5;
    public float fireInterval = 1.2f;

    [Header("Refs")]
    public Transform player;            // 拖玩家 Transform（或运行时寻找）
    public Transform firePoint;         // 子弹发射位置
    public GameObject bulletPrefab;     // 需要带 Rigidbody 的子弹预制体

    private int _hp;
    private Coroutine _fireCo;
    private Collider _bossCollider;

    void Awake()
    {
        _hp = maxHP;
        _bossCollider = GetComponent<Collider>();
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void OnEnable()
    {
        if (_fireCo == null) _fireCo = StartCoroutine(FireLoop());
    }

    void OnDisable()
    {
        if (_fireCo != null)
        {
            StopCoroutine(_fireCo);
            _fireCo = null;
        }
    }

    IEnumerator FireLoop()
    {
        var wait = new WaitForSeconds(fireInterval);
        while (true)
        {
            yield return wait;
            TryFire();
        }
    }

    void TryFire()
    {
        if (player == null || bulletPrefab == null || firePoint == null) return;

        // 计算发射方向（发射瞬间锁定玩家位置）
        Vector3 dir = (player.position - firePoint.position).normalized;

        var go = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir, Vector3.up));
        var bullet = go.GetComponent<ReflectableBullet>();
        if (bullet != null)
        {
            // 初始化子弹，传入初速度方向 & 忽略自身碰撞的时间
            bullet.Init(
                initialDir: dir,
                ownerToIgnore: _bossCollider,
                ownerIgnoreTime: 0.2f   // 生成后 0.2s 内忽略与 Boss 碰撞
            );
        }
    }

    /// <summary>被反弹子弹命中时调用。</summary>
    public void TakeDamage(int amount = 1)
    {
        _hp -= Mathf.Max(1, amount);
        _hp = Mathf.Max(0, _hp);
        Debug.Log($"[Boss] hit! HP: {_hp}/{maxHP}");

        if (_hp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[Boss] Dead!");
        // 停止开火，播放动画/掉落/传送门开启等
        if (_fireCo != null)
        {
            StopCoroutine(_fireCo);
            _fireCo = null;
        }
        // 这里可以触发动画或事件；示例简单处理：
        Destroy(gameObject, 0.25f);
    }
}
