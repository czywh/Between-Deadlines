using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PortalConditionalSpawner : MonoBehaviour
{
    [Header("References")]
   
    public PortalSphereController portal;
   
    public string playerTag = "Player";

    [Header("Spawn Settings")]
    
    public GameObject[] prefabsToSpawn;
  
    public Transform[] spawnPoints;
    public bool spawnOnce = true;
   
    public bool despawnWhenLeaveSustain = false;

    [Header("Timing")]
    
    public float perItemInterval = 0.2f;
    
    public float spawnCooldown = 10f;

    [Header("Rules")]
  
    public bool requireSustainingDuringSpawn = true;

    
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

    
    private void TrySpawn()
    {
        if (_spawnCo != null) return;                      
        if (spawnOnce && _spawned) return;                 
        if (!CanSpawnNow()) return;                       

        _spawnCo = StartCoroutine(SpawnRoutine());
    }

   
    private bool CanSpawnNow()
    {
        
        if (spawnCooldown > 0f && (Time.time - _lastSpawnTime) < spawnCooldown)
            return false;

       
        if (requireSustainingDuringSpawn)
        {
            if (portal == null || !portal.IsSustaining)
                return false;
        }

        if (!_playerOnTop)
            return false;

       
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0)
            return false;

        return true;
    }

   
    private IEnumerator SpawnRoutine()
    {
       
        if (!CanSpawnNow())
        {
            _spawnCo = null;
            yield break;
        }

        int count = prefabsToSpawn.Length;

        for (int i = 0; i < count; i++)
        {
           
            if (requireSustainingDuringSpawn)
            {
                if (portal == null || !portal.IsSustaining)
                    break; 
            }

           
            if (!_playerOnTop)
                break;

            GameObject prefab = prefabsToSpawn[i];
            if (prefab == null)
            {
                
                if (perItemInterval > 0f) yield return new WaitForSeconds(perItemInterval);
                continue;
            }

            
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i] != null)
            {
                pos = spawnPoints[i].position;
                rot = spawnPoints[i].rotation;
            }

            var go = Instantiate(prefab, pos, rot);
            _spawnedObjs.Add(go);

            
            if (perItemInterval > 0f)
                yield return new WaitForSeconds(perItemInterval);
            else
                yield return null; 
        }

        _lastSpawnTime = Time.time;
        _spawned = true;

        _spawnCo = null;
    }

    
    private void DespawnAll()
    {
        for (int i = _spawnedObjs.Count - 1; i >= 0; i--)
        {
            if (_spawnedObjs[i] != null)
                Destroy(_spawnedObjs[i]);
        }
        _spawnedObjs.Clear();

       
    }
}