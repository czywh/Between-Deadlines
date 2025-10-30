using UnityEngine;

public class BlockBehaviour : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private bool explodeOnLanding = false;
    public bool ExplodeOnLanding => explodeOnLanding;

    [SerializeField] private Color greyTint = new Color(0.6f, 0.6f, 0.6f);

    [Header("FX")]
    [SerializeField] private ParticleSystem collisionParticles;
    [SerializeField] private Renderer rend;               // 可拖拽 MeshRenderer（留空则自动搜）
    [SerializeField] private Material firstJumpMaterial;  // 第一次踩后使用的材质
    [SerializeField] private GameObject fireParticlePrefab;   // 常驻火焰（子物体）
    [SerializeField] private GameObject fireExplosionPrefab;  // 破碎爆炸

    [Header("Motion (optional)")]
    [SerializeField] private bool canFloat = true;
    private float floatSpeed;
    private float floatAmplitude;
    private Vector3 startPos;

    // platform carry delta
    public Vector3 Delta { get; private set; } = Vector3.zero;
    private Vector3 _lastPos;

    // step state
    // 0 = untouched, 1 = activated, 2 = broken
    private int steps = 0;
    public bool IsActivated => steps >= 1;
    public bool IsBroken => steps >= 2;

    private Color baseColor;
    private Material originalMaterial;

    private GameObject currentFire;
    private bool firstJumpDone = false;

    void Awake()
    {
        if (!rend) rend = GetComponentInChildren<Renderer>();
        if (rend)
        {
            // 缓存原始材质与颜色，便于复位
            originalMaterial = rend.material;
            baseColor = originalMaterial.HasProperty("_BaseColor")
                ? originalMaterial.GetColor("_BaseColor")
                : (originalMaterial.HasProperty("_Color") ? originalMaterial.GetColor("_Color") : rend.material.color);
        }

        // —— 编辑器中的视觉提示：即爆块默认染成灰色（仅提示）
        if (explodeOnLanding && rend)
        {
            // 仅改颜色，不替换材质实例
            if (rend.material.HasProperty("_BaseColor"))
                rend.material.SetColor("_BaseColor", greyTint);
            else if (rend.material.HasProperty("_Color"))
                rend.material.SetColor("_Color", greyTint);
            else
                rend.material.color = greyTint;
        }
    }

    void Start()
    {
        startPos = transform.position;
        floatSpeed = Random.Range(1.5f, 2.5f);
        floatAmplitude = Random.Range(0.1f, 0.3f);
        _lastPos = transform.position;

        // ★ 即爆块：从一开始就保持火焰常驻
        if (explodeOnLanding)
            EnsureFireLoop();
    }

    void Update()
    {
        /* 如需漂浮，解开注释
        if (canFloat && !IsBroken)
        {
            float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = startPos + new Vector3(0, offset, 0);
        }
        */

        Delta = transform.position - _lastPos;
        _lastPos = transform.position;
    }

    public void OnPortalActive(bool active)
    {
        // pause/resume floating and re-anchor so it doesn't snap
        canFloat = active;
        startPos = transform.position;
        _lastPos = transform.position;
        Delta = Vector3.zero;

        if (!active)
        {
            // —— 关闭时：重置外观&状态，移除火焰
            steps = 0;
            firstJumpDone = false;

            if (rend)
            {
                // 还原原始材质与颜色
                if (originalMaterial != null) rend.material = originalMaterial;

                if (rend.material.HasProperty("_BaseColor"))
                    rend.material.SetColor("_BaseColor", baseColor);
                else if (rend.material.HasProperty("_Color"))
                    rend.material.SetColor("_Color", baseColor);
                else
                    rend.material.color = baseColor;

                rend.enabled = true;
            }

            if (currentFire != null)
            {
                Destroy(currentFire);
                currentFire = null;
            }

            var col = GetComponent<Collider>();
            if (col) col.enabled = true;
        }
        else
        {
            // —— 重新开启：若是即爆块则恢复常驻火焰
            if (explodeOnLanding)
                EnsureFireLoop();
        }
    }

    public void ApplyFirstStepIfNeeded()
    {
        Debug.Log($"[Block] FirstStep? explodeOnLanding={explodeOnLanding} steps(before)={steps}", this);

        // 即爆块不进入第一步流程（立即爆的逻辑由外部或第二步触发）
        if (explodeOnLanding) return;

        if (IsBroken || IsActivated) return;
        steps = 1;

        if (rend && firstJumpMaterial != null)
            rend.material = firstJumpMaterial;

        if (!firstJumpDone && fireParticlePrefab != null)
        {
            currentFire = Instantiate(fireParticlePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity, transform);
            firstJumpDone = true;
        }

        Debug.Log($"[Block] FirstStep APPLIED steps(now)={steps}", this);
    }

    public void ApplySecondStepIfReady()
    {
        if (IsBroken || !IsActivated) return; // only after first step
        BreakNow();
    }

    public void BreakNow()   // must be public
    {
        Debug.Log($"[Block] BreakNow CALLED steps(before)={steps} IsBroken={IsBroken}", this);

        if (IsBroken) return;
        steps = 2;

        // 停止常驻火焰
        if (currentFire != null)
        {
            Destroy(currentFire);
            currentFire = null;
        }

        // 爆炸/碎裂特效（二选一）
        if (fireExplosionPrefab != null)
        {
            Instantiate(fireExplosionPrefab, transform.position, Quaternion.identity);
        }
        else if (collisionParticles)
        {
            var fx = Instantiate(collisionParticles, transform.position, Quaternion.identity);
            var main = fx.main;
            if (rend)
            {
                Color useColor = baseColor;
                // 取当前材质颜色作为粒子主色（尽量兼容）
                if (rend.material.HasProperty("_BaseColor")) useColor = rend.material.GetColor("_BaseColor");
                else if (rend.material.HasProperty("_Color")) useColor = rend.material.GetColor("_Color");
                main.startColor = useColor;
            }
            Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.25f);
        }

        // 关闭碰撞与渲染
        foreach (var c in GetComponentsInChildren<Collider>(true))  c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true))  r.enabled = false;

        Destroy(gameObject, 1f);
    }

    // —— 常驻火焰保证：若没有就生成 —— //
    private void EnsureFireLoop()
    {
        if (fireParticlePrefab != null && currentFire == null)
        {
            currentFire = Instantiate(
                fireParticlePrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity,
                transform // 作为子物体，跟随方块移动/销毁
            );
        }
    }
}