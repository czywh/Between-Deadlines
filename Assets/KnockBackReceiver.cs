using UnityEngine;

[DisallowMultipleComponent]
public class KnockbackReceiver : MonoBehaviour
{
    [Header("Tuning")]
    [Tooltip("击退初始强度的衰减系数，数值越大，消退越快")]
    public float decayPerSecond = 6f;
    [Tooltip("向上分量比例（0~1）")]
    public float upFactor = 0.15f;
    [Tooltip("是否忽略来源方向的Y分量（多数情况下是true更自然）")]
    public bool ignoreYFromSource = true;
    [Tooltip("本体对击退的抗性(0~1，越大越不容易被击退)")]
    [Range(0f, 0.95f)] public float resistance = 0f;

    // 如果你是用 CharacterController.Move，设为 true；如果用 rb.velocity/MovePosition，可设为 false，仅做向量输出
    [Header("Driver")]
    public bool driveCharacterController = false;

    private Vector3 knockVel;   // 当前累计的击退速度(世界向量)
    private CharacterController cc;
    private Rigidbody rb;

    public bool IsActive => knockVel.sqrMagnitude > 0.0001f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 从“来源点”对玩家施加一次击退。force为“速度增量”的标称值（可按需与质量/帧率联动）
    /// </summary>
    public void AddKnockback(Vector3 sourcePosition, float force, float? overrideUpFactor = null)
    {
        var dir = (transform.position - sourcePosition);
        if (ignoreYFromSource) dir.y = 0f;
        dir = dir.sqrMagnitude < 0.0001f ? transform.forward : dir.normalized;

        float up = (overrideUpFactor ?? upFactor);
        Vector3 impulse = dir * force * (1f - resistance) + Vector3.up * force * up;
        knockVel += impulse; // 累加（可多次受击）
    }

    /// <summary>
    /// 直接按方向施加击退（如果你已经算好了方向）
    /// </summary>
    public void AddKnockbackDir(Vector3 worldDirNormalized, float force, float? overrideUpFactor = null)
    {
        Vector3 dir = worldDirNormalized.sqrMagnitude < 0.0001f ? transform.forward : worldDirNormalized.normalized;
        float up = (overrideUpFactor ?? upFactor);
        Vector3 impulse = dir * force * (1f - resistance) + Vector3.up * force * up;
        knockVel += impulse;
    }

    /// <summary>
    /// 供“速度合成”型移动调用：返回本帧应叠加到移动/速度上的击退量（并做衰减）
    /// </summary>
    public Vector3 ConsumeDelta(float dt)
    {
        // 把当前击退速度转成本帧位移量（或速度增量由外部决定）
        Vector3 delta = knockVel * dt;

        // 指数式衰减（平滑）
        float k = Mathf.Exp(-decayPerSecond * dt);
        knockVel *= k;

        // 很小就归零，避免尾巴
        if (knockVel.sqrMagnitude < 1e-4f) knockVel = Vector3.zero;

        return delta;
    }

    void Update()
    {
        // 如果你用 CharacterController.Move 并希望组件自驱动（不改你原脚本）
        if (driveCharacterController && cc != null)
        {
            Vector3 delta = ConsumeDelta(Time.deltaTime);
            if (delta.sqrMagnitude > 0f)
                cc.Move(delta);
        }
    }
}