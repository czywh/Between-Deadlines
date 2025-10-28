using UnityEngine;

public class PlayerHitReceiver : MonoBehaviour
{
    public Vector3 localKnockbackDir = new Vector3(1f, 0f, -1f); // 右后方
    public float knockbackSpeed = 18f;    // 外力初速度
    public float knockbackDuration = 0.25f; // 若用持续时间接口

    private PlayerMovement _move;

    void Awake()
    {
        _move = GetComponentInParent<PlayerMovement>(); // 或 GetComponent
    }

    void OnCollisionEnter(Collision collision)
{
    if (collision.collider.CompareTag("Bullet"))
    {
        Debug.Log($"[PlayerHitReceiver] OnCollisionEnter with {collision.collider.name}");
        // 1) 掉血
        var hp = GetComponentInParent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(1);

        // 2) 击退（你原有的调用保持不变）
        DoKnockback();

        // 3) 销毁子弹（拿父级以防子弹碰撞体在子节点）
        var bullet = collision.collider.GetComponentInParent<ReflectableBullet>();
        if (bullet != null) Destroy(bullet.gameObject);
        else Destroy(collision.collider.gameObject);
    }
}

void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Bullet"))
    {
        Debug.Log($"[PlayerHitReceiver] OnTriggerEnter with {other.name}");
        var hp = GetComponentInParent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(1);

        DoKnockback();

        var bullet = other.GetComponentInParent<ReflectableBullet>();
        if (bullet != null) Destroy(bullet.gameObject);
        else Destroy(other.gameObject);
    }
}

    private void DoKnockback()
    {
        // 把“本地右后方”转成世界方向
        Vector3 worldDir = transform.TransformDirection(localKnockbackDir).normalized;
        // 二选一：
        _move.AddExternalImpulse(worldDir, knockbackSpeed);                      // 衰减型
        
    }
}