// ProximityTriggerSnapper.cs
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]   // 在编辑器里也能生效，方便摆位
#endif
public class ProximityTriggerSnapper : MonoBehaviour
{
    [Header("Target（不填就用父物体）")]
    public Transform target;

    [Header("摆位参数")]
    public float distance = 1.0f;           // 贴在目标“前方”多少米
    public float yOffset  = 0.8f;           // 往上抬
    public bool alignToTargetForward = true;// 以目标的 forward 为参照（否则用世界Z轴）
    public Vector3 localNudge = Vector3.zero; // 贴过去后再本地微调

    [Header("跟随")]
    public bool followContinuously = false; // 只想“贴一次”，就保持 false

    void OnEnable() { SnapNow(); }
    void Update() { if (followContinuously) SnapNow(); }

    [ContextMenu("Snap Now")]
    public void SnapNow()
    {
        var t = target ? target : transform.parent;
        if (!t) return;

        // 选择朝向
        Vector3 fwd = alignToTargetForward ? t.forward : Vector3.forward;

        // 计算目标中心（优先 Renderer 包围盒）
        Vector3 center = t.position;
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend) center = rend.bounds.center;

        // 贴到前方 + 抬高 + 本地微调
        transform.position = center + fwd * distance + Vector3.up * yOffset;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        transform.position += transform.TransformVector(localNudge);
    }
}
