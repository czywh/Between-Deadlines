using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHP = 5;
    public int currentHP = 5;

    [Header("Damage Control")]
    [Tooltip("受击后短暂无敌（秒），防止一帧多次结算")]
    public float invulnerableTime = 0.2f;

    [Header("Death")]
    [Tooltip("死亡后重开整局的延迟（秒）")]
    public float restartDelay = 3f;
    [Tooltip("死亡时禁用的组件（移动/输入等）")]
    public Behaviour[] componentsToDisableOnDeath;
    [Tooltip("死亡时播放的粒子（可空，走粒子自身生命周期）")]
    public GameObject deathEffectPrefab;

    private bool _dead = false;
    private float _invUntil = 0f;

    void Awake()
    {
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    public void TakeDamage(int amount = 1)
    {
        if (_dead) return;
        if (Time.time < _invUntil) return;

        amount = Mathf.Max(1, amount);
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        _invUntil = Time.time + invulnerableTime;

        // TODO: 这里可以加屏幕闪红/音效/受击动画
        Debug.Log($"[PlayerHealth] Hit! HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        if (_dead) return;
        _dead = true;
        Debug.Log("[PlayerHealth] Player dead. Restart in " + restartDelay + "s");

        // 播放死亡粒子（不定时销毁，交给粒子系统 Stop Action 或自回收脚本）
        if (deathEffectPrefab != null)
        {
            var fx = Instantiate(deathEffectPrefab, transform.position, transform.rotation);
            // 若你的粒子没设 Stop Action=Destroy，可在粒子 prefab 上加 AutoDestroyWhenParticlesDone
        }

        // 禁用移动/输入等
        if (componentsToDisableOnDeath != null)
        {
            foreach (var b in componentsToDisableOnDeath)
                if (b) b.enabled = false;
        }

        // 可选：把碰撞体关掉，避免尸体继续触发
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // 3 秒后重开当前场景
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        var scene = SceneManager.GetActiveScene();
        // 确保时间缩放被重置
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene.buildIndex);
    }

    // 便于其它脚本查询
    public bool IsDead => _dead;
}