using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class PlaceHelpAddon : MonoBehaviour
{
    [Header("Target (默认取父物体)")]
    public Transform target;                 // 不填就用 transform.parent

    [Header("摆位参数")]
    [Tooltip("沿着 target 的前方向前多少米")]
    public float distance = 1.0f;
    [Tooltip("离地/相对target中心向上多少米")]
    public float yOffset = 1.0f;
    [Tooltip("用于确定“中心”的方式：优先用渲染包围盒")]
    public bool useRendererBounds = true;
    [Tooltip("把 HelpAddon 旋转到正对玩家/相机；不勾选则正对 target.forward 反向")]
    public bool faceCamera = false;

    [Header("局部微调")]
    public Vector3 localRotationOffsetEuler = new Vector3(0, 0, 0);
    public Vector3 localPositionNudge = Vector3.zero;

    // ——可选：运行时是否持续跟随
    public bool followContinuously = false;

    void OnEnable()
    {
        if (target == null && transform.parent != null) target = transform.parent;
        SnapInFront();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying && followContinuously)
            SnapInFront();
    }
#endif

    public void SnapInFront()
    {
        if (target == null) return;

        // 1) 计算参考中心点
        Vector3 center = target.position;
        float halfHeight = 1.0f;

        if (useRendererBounds)
        {
            var rens = target.GetComponentsInChildren<Renderer>();
            if (rens.Length > 0)
            {
                Bounds b = rens[0].bounds;
                foreach (var r in rens) b.Encapsulate(r.bounds);
                center = b.center;
                halfHeight = b.extents.y;
            }
        }

        // 2) 取“前方”方向
        Vector3 fwd = target.forward; // 以物体自身forward为“正前方”
        if (fwd == Vector3.zero) fwd = Vector3.forward;

        // 3) 目标位置：中心 + 向上 + 向前
        Vector3 pos = center + Vector3.up * (yOffset + halfHeight * 0.0f) + fwd.normalized * distance;
        transform.position = pos + localPositionNudge;

        // 4) 朝向：正对相机 或 面向 target
        Quaternion rot;
        if (faceCamera && Camera.main != null)
        {
            Vector3 toCam = (Camera.main.transform.position - transform.position);
            toCam.y = 0; // 保持水平朝向
            if (toCam.sqrMagnitude < 0.0001f) toCam = -fwd;
            rot = Quaternion.LookRotation(toCam, Vector3.up);
        }
        else
        {
            // 让面板朝向 target（让面板“看着”物体），因此方向取 -fwd
            rot = Quaternion.LookRotation(-fwd, Vector3.up);
        }

        transform.rotation = rot * Quaternion.Euler(localRotationOffsetEuler);
    }

    // 右键菜单：手动一键吸附
    [ContextMenu("Snap In Front Now")]
    public void SnapInFrontNow()
    {
        SnapInFront();
    }
}