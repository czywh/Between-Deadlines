using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class BossShooter : MonoBehaviour
{
    [Header("Boss Stats")]
    [Tooltip("最大生命值")]
    public int maxHP = 5;
    [Tooltip("两次发射间隔（秒）")]
    public float fireInterval = 1.2f;

    [Header("References")]
    [Tooltip("玩家 Transform（留空则按 Tag=Player 自动查找）")]
    public Transform player;
    [Tooltip("子弹发射点")]
    public Transform firePoint;
    [Tooltip("子弹预制体（需要带 ReflectableBullet 脚本+Rigidbody+Collider）")]
    public GameObject bulletPrefab;

    [Header("Hit Feedback")]
    [Tooltip("Boss 被击中时播放的粒子（命中点）")]
    public GameObject hitEffectPrefab;
    [Tooltip("需要闪白/变色的渲染器列表")]
    public Renderer[] flashRenderers;
    [Tooltip("受击闪烁颜色")]
    public Color flashColor = Color.white;
    [Tooltip("受击闪烁时长（秒）")]
    public float flashDuration = 0.08f;

    [Header("Death Effect (no fixed lifetime)")]
    [Tooltip("Boss 被摧毁时播放的粒子（使用粒子系统自己的生命周期，例如 Stop Action=Destroy）")]
    public GameObject deathEffectPrefab;

    // —— 内部状态 ——
    private int _hp;
    private Coroutine _fireCo;
    private Collider _bossCollider;

    // 用于给不同渲染器安全改色
    private MaterialPropertyBlock _mpb;
    // 根据你的着色器改成合适的颜色属性名：URP/HDRP 常用 _BaseColor，内置标准管线常用 _Color
    private static readonly int _ColorProp = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        _hp = maxHP;
        _bossCollider = GetComponent<Collider>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        _mpb = new MaterialPropertyBlock();

        // 建议在 Inspector 给 Boss 物体设置 Tag=Boss（配合你的子弹反射后按 Tag 追踪）
        // gameObject.tag = "Boss"; // 如需强制设置，可取消注释
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

    // —— 发射循环 ——
    IEnumerator FireLoop()
    {
        var wait = new WaitForSeconds(fireInterval);
        while (true)
        {
            yield return wait;
            TryFire();
        }
    }

    // —— 尝试发射一发 —— 
    void TryFire()
    {
        if (player == null || bulletPrefab == null || firePoint == null) return;

        // 锁定发射方向（瞬时）
        Vector3 dir = (player.position - firePoint.position).normalized;

        // 生成子弹
        var go = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir, Vector3.up));

        // 初始化子弹：传入方向 & 短暂忽略与 Boss 的碰撞，避免自伤
        var bullet = go.GetComponent<ReflectableBullet>();
        if (bullet != null)
        {
            bullet.Init(
                initialDir: dir,
                ownerToIgnore: _bossCollider,
                ownerIgnoreTime: 0.2f
            );
        }
    }

    // —— 受伤（带命中点）——
    public void TakeDamage(int amount, Vector3 hitPoint)
    {
        _hp -= Mathf.Max(1, amount);
        _hp = Mathf.Max(0, _hp);

        // 受击粒子（在命中点）
        if (hitEffectPrefab)
        {
            var fx = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            // 不手动 Destroy：由粒子系统自身生命周期/Stop Action 管理
        }

        // 闪白/变色反馈（不改变位置）
        if (flashRenderers != null && flashRenderers.Length > 0)
            StartCoroutine(FlashOnce());

        if (_hp <= 0) Die();
    }

    // —— 兼容：无命中点版本 —— 
    public void TakeDamage(int amount) => TakeDamage(amount, transform.position + Vector3.up * 1f);

    // —— 死亡逻辑 —— 
    private void Die()
    {
        // 停止开火
        if (_fireCo != null)
        {
            StopCoroutine(_fireCo);
            _fireCo = null;
        }

        // 死亡粒子（不设播放时长；和墙同逻辑：完全由粒子系统自己决定寿命）
        if (deathEffectPrefab != null)
        {
            // 不作为 Boss 子物体，避免 Boss 销毁把粒子一并删掉
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
            
        }

        // 稍作延迟以便受击/粒子能完整触发
        Destroy(gameObject, 0.25f);
    }

    // —— 一次闪白 —— 
    private IEnumerator FlashOnce()
    {
        // 记录原色
        Color[] orig = new Color[flashRenderers.Length];

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            var r = flashRenderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            

            _mpb.SetColor(_ColorProp, flashColor);
            r.SetPropertyBlock(_mpb);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            var r = flashRenderers[i];
            if (!r) continue;

            _mpb.SetColor(_ColorProp, orig[i]);
            r.SetPropertyBlock(_mpb);
        }
    }
}