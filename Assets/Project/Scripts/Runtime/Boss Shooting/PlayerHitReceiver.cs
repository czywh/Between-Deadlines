using UnityEngine;

public class PlayerHitReceiver : MonoBehaviour
{
    public Vector3 localKnockbackDir = new Vector3(1f, 0f, -1f); 
    public float knockbackSpeed = 18f;    
    public float knockbackDuration = 0.25f; 

   

    void Awake()
    {
        
    }

    void OnCollisionEnter(Collision collision)
{
    if (collision.collider.CompareTag("Bullet"))
    {
        Debug.Log($"[PlayerHitReceiver] OnCollisionEnter with {collision.collider.name}");
      

       
        DoKnockback();

       
        var bullet = collision.collider.GetComponentInParent<ReflectableBullet>();
        if (bullet != null) Destroy(bullet.gameObject);
        else Destroy(collision.collider.gameObject);
    }
}

void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Bullet"))
    {
       

        DoKnockback();

        var bullet = other.GetComponentInParent<ReflectableBullet>();
        if (bullet != null) Destroy(bullet.gameObject);
        else Destroy(other.gameObject);
    }
}

    private void DoKnockback()
    {
        
        Vector3 worldDir = transform.TransformDirection(localKnockbackDir).normalized;
       
        
    }
}