using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 挂在“可反弹/会暂时消失的墙”上：
/// - 要求墙体拥有非触发器 Collider（isTrigger=false）
/// - 命中后反弹子弹，并隐藏自身（禁用渲染器与碰撞体）
/// - 若干秒后自动恢复
/// </summary>
[RequireComponent(typeof(Collider))]
public class ReflectWall : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("被击中后，墙体多久再次出现（秒）")]
    public float respawnDelay = 3f;

    [Header("Optional")]
    [Tooltip("命中瞬间播放的特效（可空）")]
    public GameObject hitEffect;
    [Tooltip("在隐藏与重生时切换的额外对象（可空）")]
    public GameObject[] extraObjectsToToggle;

    private List<Renderer> _renderers = new();
    private List<Collider> _colliders = new();
    private bool _active = true;   // 墙体当前是否可用（可反弹）

    void Awake()
    {
        // 缓存所有子层级渲染器/碰撞体，统一管理显隐
        GetComponentsInChildren(true, _renderers);
        GetComponentsInChildren(true, _colliders);

        // 要求本体使用“非Trigger”的Collider，这样能拿到准确接触法线
        foreach (var col in _colliders)
        {
            if (col.isTrigger)
                Debug.LogWarning($"[ReflectWall] {name} 的某些 Collider 是 Trigger。建议使用非Trigger来获取准确法线。");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_active) return;

        // 只有子弹碰到才处理
        var bullet = collision.collider.GetComponentInParent<ReflectableBullet>();
        if (bullet == null) return;

        // 计算反弹法线（使用第一个接触点法线）
        Vector3 n = collision.GetContact(0).normal;

        // 让子弹完成反弹
        bullet.ReflectBy(n);

        // 播放命中特效（可选）
        if (hitEffect != null)
            Instantiate(hitEffect, collision.GetContact(0).point, Quaternion.LookRotation(n));

        // 墙体进入“冷却”：隐藏并禁用
        StartCoroutine(DisappearThenRespawn());
    }

    private IEnumerator DisappearThenRespawn()
    {
        SetActive(false);
        yield return new WaitForSeconds(respawnDelay);
        SetActive(true);
    }

    private void SetActive(bool enabled)
    {
        _active = enabled;
        foreach (var r in _renderers) if (r) r.enabled = enabled;
        foreach (var c in _colliders) if (c) c.enabled = enabled;

        if (extraObjectsToToggle != null)
        {
            foreach (var go in extraObjectsToToggle)
                if (go) go.SetActive(enabled);
        }
    }
}