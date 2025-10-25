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

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag("Bullet")) DoKnockback();
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet")) DoKnockback();
    }

    private void DoKnockback()
    {
        // 把“本地右后方”转成世界方向
        Vector3 worldDir = transform.TransformDirection(localKnockbackDir).normalized;
        // 二选一：
        _move.AddExternalImpulse(worldDir, knockbackSpeed);                      // 衰减型
        
    }
}