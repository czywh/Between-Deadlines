using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PortalConditionalSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("拖入你的 PortalSphereController")]
    public PortalSphereController portal;
    [Tooltip("玩家标签")]
    public string playerTag = "Player";

    [Header("Spawn Settings")]
    [Tooltip("要生成的物体预制体（按顺序逐个生成）")]
    public GameObject[] prefabsToSpawn;
    [Tooltip("对应每个预制体的生成点（可留空或数量不足，回退到本物体位置/rotation）")]
    public Transform[] spawnPoints;
    [Tooltip("只生成一次")]
    public bool spawnOnce = true;
    [Tooltip("Sustaining 结束时是否回收已生成的物体")]
    public bool despawnWhenLeaveSustain = false;

    [Header("Timing")]
    [Tooltip("每个物体之间的生成间隔（秒）")]
    public float perItemInterval = 0.2f;
    [Tooltip("整体生成的冷却时间（秒），为 0 则无冷却")]
    public float spawnCooldown = 10f;

    [Header("Rules")]
    [Tooltip("生成期间是否要求始终处于 Sustaining 状态；为 true 时，一旦状态中断就停止继续生成")]
    public bool requireSustainingDuringSpawn = true;

    // --- 内部状态 ---
    private bool _playerOnTop = false;
    private bool _spawned = false;

    private float _lastSpawnTime = -Mathf.Infinity;
    private Coroutine _spawnCo;

    private readonly List<GameObject> _spawnedObjs = new();

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (portal != null)
        {
            portal.OnSustainingStarted.AddListener(HandleSustainStart);
            portal.OnSustainingEnded.AddListener(HandleSustainEnd);
        }
    }

    private void OnDisable()
    {
        if (portal != null)
        {
            portal.OnSustainingStarted.RemoveListener(HandleSustainStart);
            portal.OnSustainingEnded.RemoveListener(HandleSustainEnd);
        }

        // 组件禁用时停止协程，避免悬挂
        if (_spawnCo != null)
        {
            StopCoroutine(_spawnCo);
            _spawnCo = null;
        }
    }

    private void HandleSustainStart()
    {
        if (_playerOnTop)
            TrySpawn();
    }

    private void HandleSustainEnd()
    {
        // 若要求 Sustaining 期间才能继续生成，则中断协程
        if (requireSustainingDuringSpawn && _spawnCo != null)
        {
            StopCoroutine(_spawnCo);
            _spawnCo = null;
        }

        if (despawnWhenLeaveSustain)
            DespawnAll();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            _playerOnTop = true;
            if (portal != null && portal.IsSustaining)
                TrySpawn();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            _playerOnTop = false;
    }

    /// <summary>
    /// 外部/事件触发的尝试生成入口
    /// </summary>
    private void TrySpawn()
    {
        if (_spawnCo != null) return;                      // 已在生成中
        if (spawnOnce && _spawned) return;                 // 只生成一次
        if (!CanSpawnNow()) return;                        // 冷却或规则不满足

        _spawnCo = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// 是否满足生成前置条件
    /// </summary>
    private bool CanSpawnNow()
    {
        // 冷却判断
        if (spawnCooldown > 0f && (Time.time - _lastSpawnTime) < spawnCooldown)
            return false;

        // Sustaining 规则
        if (requireSustainingDuringSpawn)
        {
            if (portal == null || !portal.IsSustaining)
                return false;
        }

        // 玩家必须在触发区内
        if (!_playerOnTop)
            return false;

        // 有待生成对象
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0)
            return false;

        return true;
    }

    /// <summary>
    /// 逐个生成的协程
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        // 再次保护：开始时条件检查
        if (!CanSpawnNow())
        {
            _spawnCo = null;
            yield break;
        }

        int count = prefabsToSpawn.Length;

        for (int i = 0; i < count; i++)
        {
            // 进行中若要求 Sustaining，则每步都校验
            if (requireSustainingDuringSpawn)
            {
                if (portal == null || !portal.IsSustaining)
                    break; // 中断剩余生成
            }

            // 玩家离开则也停止
            if (!_playerOnTop)
                break;

            GameObject prefab = prefabsToSpawn[i];
            if (prefab == null)
            {
                // 跳过空位，但仍然等待一下，保持节奏（也可以选择不等待）
                if (perItemInterval > 0f) yield return new WaitForSeconds(perItemInterval);
                continue;
            }

            // 位置和旋转：有对应 spawnPoint 用它，没有就用自己 transform
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i] != null)
            {
                pos = spawnPoints[i].position;
                rot = spawnPoints[i].rotation;
            }

            var go = Instantiate(prefab, pos, rot);
            _spawnedObjs.Add(go);

            // 单个生成后的间隔
            if (perItemInterval > 0f)
                yield return new WaitForSeconds(perItemInterval);
            else
                yield return null; // 最少让出一帧
        }

        _lastSpawnTime = Time.time;
        _spawned = true;

        _spawnCo = null;
    }

    /// <summary>
    /// 回收所有已生成对象
    /// </summary>
    private void DespawnAll()
    {
        for (int i = _spawnedObjs.Count - 1; i >= 0; i--)
        {
            if (_spawnedObjs[i] != null)
                Destroy(_spawnedObjs[i]);
        }
        _spawnedObjs.Clear();

        // 若允许多次生成，这里不重置 _spawned；但 spawnOnce==true 的语义是“只生成一次”，
        // 通常不希望 Sustaining 结束后还能再来一次，因此保留 _spawned=true。
        // 如果你希望回收后可以再次生成，把下一行取消注释：
        // _spawned = false;
    }
}