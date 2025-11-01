using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(Collider))]
public class ReflectWall : MonoBehaviour
{
    public float respawnDelay = 3f;

   
    public GameObject hitEffect;
   
    public GameObject[] extraObjectsToToggle;

    private List<Renderer> _renderers = new();
    private List<Collider> _colliders = new();
    private bool _active = true;   

    void Awake()
    {
       
        GetComponentsInChildren(true, _renderers);
        GetComponentsInChildren(true, _colliders);

        
        foreach (var col in _colliders)
        {
            if (col.isTrigger)
                Debug.LogWarning($"[ReflectWall] {name} 的某些 Collider 是 Trigger。建议使用非Trigger来获取准确法线。");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_active) return;

      
        var bullet = collision.collider.GetComponentInParent<ReflectableBullet>();
        if (bullet == null) return;

        
        Vector3 n = collision.GetContact(0).normal;

       
        bullet.ReflectBy(n);

        
        if (hitEffect != null)
            Instantiate(hitEffect, collision.GetContact(0).point, Quaternion.LookRotation(n));

        
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