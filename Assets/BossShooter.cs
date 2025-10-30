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

    [Header("Hit Shake")]
    [Tooltip("Boss 被击中时抖动的幅度（单位）")]
    public float shakeIntensity = 0.2f;
    [Tooltip("Boss 被击中时抖动的持续时间（秒）")]
    public float shakeDuration = 0.15f;

    [Header("Aggro / Activation")]
    [Tooltip("玩家进入该距离内时激活 Boss 攻击")]
    public float activateRange = 12f;
    [Tooltip("玩家离开到该距离外时取消激活（迟滞，避免反复抖动）")]
    public float deactivateRange = 14f;
    [Tooltip("是否需要与玩家有直线视野才算激活")]
    public bool requireLineOfSight = false;
    [Tooltip("视线检测的阻挡层（地形/墙等）。留空=默认射线命中一切。")]
    public LayerMask losObstacles = ~0;

    // —— 内部状态 ——
    private int _hp;
    private Coroutine _fireCo;
    private Collider _bossCollider;

    // 受击变色（MPB）
    private MaterialPropertyBlock _mpb;
    private static readonly int _ColorPropBase = Shader.PropertyToID("_BaseColor");
    private static readonly int _ColorPropStd = Shader.PropertyToID("_Color");
    private Color[] _origColors; // 每个 renderer 的原始颜色缓存

    // 激活开关
    private bool _aggro = false;

    public int CurrentHP => _hp;
    public int MaxHP => maxHP;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color activateColor = new Color(0f, 1f, 0.6f, 1f);
    public Color deactivateColor = new Color(1f, 0.7f, 0f, 1f);
    public bool drawWhenUnselected = false; // 勾选则未选中对象也显示

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

        // 缓存原色（若未赋 renderers 列表则跳过）
        if (flashRenderers != null && flashRenderers.Length > 0)
        {
            _origColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                var r = flashRenderers[i];
                if (!r) { _origColors[i] = Color.white; continue; }

                // 优先从 MPB 读，读不到时尝试材质常用属性
                r.GetPropertyBlock(_mpb);
                if (_mpb != null && (_mpb.GetVector(_ColorPropBase) != Vector4.zero))
                    _origColors[i] = _mpb.GetColor(_ColorPropBase);
                else if (_mpb != null && (_mpb.GetVector(_ColorPropStd) != Vector4.zero))
                    _origColors[i] = _mpb.GetColor(_ColorPropStd);
                else if (r.sharedMaterial != null)
                {
                    if (r.sharedMaterial.HasProperty(_ColorPropBase))
                        _origColors[i] = r.sharedMaterial.GetColor(_ColorPropBase);
                    else if (r.sharedMaterial.HasProperty(_ColorPropStd))
                        _origColors[i] = r.sharedMaterial.GetColor(_ColorPropStd);
                    else
                        _origColors[i] = Color.white;
                }
                else _origColors[i] = Color.white;
            }
        }

        // 可选：强制设置 Tag 以配合反弹子弹追踪
        // gameObject.tag = "Boss";
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

    void Update()
    {
        UpdateAggro();
    }

    // —— 发射循环（仅激活时才发射） ——
    IEnumerator FireLoop()
    {
        var wait = new WaitForSeconds(fireInterval);
        while (true)
        {
            yield return wait;
            if (_aggro)
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

        StartCoroutine(ShakeOnce());

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

        // 死亡粒子（不设播放时长；完全由粒子系统自己决定寿命）
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }

        // 稍作延迟以便受击/粒子能完整触发
        Destroy(gameObject, 0.25f);
    }

    // —— 一次闪白 —— 
    private IEnumerator FlashOnce()
    {
        if (flashRenderers == null || flashRenderers.Length == 0) yield break;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            var r = flashRenderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            // 优先写 _BaseColor，失败再写 _Color
            _mpb.SetColor(_ColorPropBase, flashColor);
            r.SetPropertyBlock(_mpb);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            var r = flashRenderers[i];
            if (!r) continue;

            // 还原缓存的原色
            var restore = (_origColors != null && i < _origColors.Length) ? _origColors[i] : Color.white;
            r.GetPropertyBlock(_mpb);
            if (_mpb != null)
            {
                // 两个常见属性都尝试一下，确保兼容不同管线
                _mpb.SetColor(_ColorPropBase, restore);
                _mpb.SetColor(_ColorPropStd, restore);
                r.SetPropertyBlock(_mpb);
            }
        }
    }

    private IEnumerator ShakeOnce()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // 随机方向的小幅偏移
            float offsetX = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetZ = Random.Range(-1f, 1f) * shakeIntensity * 0.5f; // 轻微Z向抖动

            transform.position = originalPos + new Vector3(offsetX, offsetY, offsetZ);

            yield return null;
        }

        transform.position = originalPos;
    }

    // —— 激活判定 —— //
    private void UpdateAggro()
    {
        if (player == null)
        {
            _aggro = false;
            return;
        }

        float d = Vector3.Distance(transform.position, player.position);

        if (_aggro)
        {
            // 已激活：距离超过“取消阈值”或（需要LOS且失去视线）则关闭
            if (d > deactivateRange || !HasLineOfSight())
                _aggro = false;
        }
        else
        {
            // 未激活：进入“激活阈值”且（如需要LOS则必须有视线）才开启
            if (d <= activateRange && HasLineOfSight())
                _aggro = true;
        }
    }

    private bool HasLineOfSight()
    {
        if (!requireLineOfSight) return true;
        if (player == null) return false;

        Vector3 origin = firePoint ? firePoint.position : transform.position;
        Vector3 toPlayer = player.position - origin;
        float dist = toPlayer.magnitude;
        if (dist <= 0.001f) return true;

        Vector3 dir = toPlayer / Mathf.Max(dist, 0.0001f);
        // 命中障碍即视为被阻挡
        return !Physics.Raycast(origin, dir, dist, losObstacles, QueryTriggerInteraction.Ignore);
    }

    // 在类末尾加：
    void OnDrawGizmos()
    {
        if (!drawWhenUnselected || !showGizmos) return;
        DrawRangesGizmo();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        DrawRangesGizmo();
    }

    private void DrawRangesGizmo()
    {
        Vector3 p = transform.position;

        // 激活半径
        Gizmos.color = activateColor;
        Gizmos.DrawWireSphere(p, Mathf.Max(0f, activateRange));

        // 取消激活半径（迟滞圈）
        Gizmos.color = deactivateColor;
        Gizmos.DrawWireSphere(p, Mathf.Max(0f, deactivateRange));

        // 可选：画向玩家的线（仅在有引用时）
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(p, player.position);
            // 小端点
            Gizmos.DrawWireSphere(player.position, 0.2f);
        }
    }

#if UNITY_EDITOR
    // 可选：在 Scene 里标注文字（不影响打包）
    private void OnSceneGUI() { } // 占位避免警告
#endif
}