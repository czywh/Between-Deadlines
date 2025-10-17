using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalConditionalSpawner : MonoBehaviour
{
    [Header("References")]
    public PortalSphereController portal;       // 拖入你的 PortalSphereController
    public string playerTag = "Player";         // 玩家标签，默认 "Player"

    [Header("Spawn Settings")]
    public GameObject[] prefabsToSpawn;         // 要生成的物体预制体
    public Transform[] spawnPoints;             // 生成位置（可为空则在本物体位置生成）
    public bool spawnOnce = true;               // 只生成一次
    public bool despawnWhenLeaveSustain = false;// Sustaining 结束后是否回收

    private bool _playerOnTop = false;
    private bool _spawned = false;
    private readonly System.Collections.Generic.List<GameObject> _spawnedObjs = new();

    private void Awake()
    {
        // 确保碰撞体是触发器
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
    }

    private void HandleSustainStart()
    {
        if (_playerOnTop)
            TrySpawn();
    }

    private void HandleSustainEnd()
    {
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

    private void TrySpawn()
    {
        if (spawnOnce && _spawned) return;

        for (int i = 0; i < prefabsToSpawn.Length; i++)
        {
            if (!prefabsToSpawn[i]) continue;

            Vector3 pos = transform.position;
            Quaternion rot = Quaternion.identity;

            if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i])
            {
                pos = spawnPoints[i].position;
                rot = spawnPoints[i].rotation;
            }

            var go = Instantiate(prefabsToSpawn[i], pos, rot);
            _spawnedObjs.Add(go);
        }

        _spawned = true;
    }

    private void DespawnAll()
    {
        for (int i = _spawnedObjs.Count - 1; i >= 0; i--)
        {
            if (_spawnedObjs[i]) Destroy(_spawnedObjs[i]);
        }
        _spawnedObjs.Clear();
        _spawned = false;
    }
}