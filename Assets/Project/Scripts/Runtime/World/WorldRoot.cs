using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class WorldRoot : MonoBehaviour
{
    public enum WorldId { A, B }

    [Header("Identity")]
    public WorldId id = WorldId.A;

    [Header("Spawn Points")]
    public Transform playerSpawn;

    [Header("Physics Overrides")]
    public bool overrideGravity = true;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Header("Time Overrides")]
    public bool overrideTimeScale = false;
    [Range(0.0f, 2.0f)] public float timeScale = 1.0f;     // 全局：谨慎使用




    [Header("References (Optional)")]
    public GameObject portalsRoot;         // 用来放 PortalPair 的节点
    public Transform[] interestPoints;     // 便于相机或AI参考


	// —— 对外暴露的便捷方法 ——
	public Vector3 GetSpawnPosition() => playerSpawn ? playerSpawn.position : transform.position;

    public void ApplyWorldSettings()
    {
        // 1. Gravity 
        if (overrideGravity)
        {
            Physics.gravity = gravity;
        }

        // 2. Time 
        if (overrideTimeScale)
        {
            Time.timeScale = Mathf.Max(0f, timeScale);
        }

       

		// 5. Shader 全局 ID (区分 A/B 世界)
		Shader.SetGlobalInt("_WorldId", (int)id);
    }
}