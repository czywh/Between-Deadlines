using UnityEngine;
using System.Collections;

public class TimedDespawn : MonoBehaviour
{
    public float lifetime = 10f;               
    public bool useOutroAnimation = true;      
    private PopInBounce _anim;

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(CoLife());
    }

    IEnumerator CoLife()
    {
        if (lifetime > 0)
            yield return new WaitForSeconds(lifetime);

        _anim = GetComponent<PopInBounce>();
        if (useOutroAnimation && _anim != null)
        {
            _anim.PlayOutAndDestroy(); 
        }
        else
        {
            Destroy(gameObject);       
        }
    }
}