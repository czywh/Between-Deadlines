using UnityEngine;

public class WorldManager : MonoBehaviour
{
	[Header("References")]
	public Transform player;
	public Camera mainCamera;
	public Camera portalCamera;
	public GameObject portalSphere;

	[Header("World Roots")]
	public WorldRoot worldA;
	public WorldRoot worldB;
	public Transform worldBOnlyRoot;
	private WorldRoot _curWorld;

	[Header("Layer Settings")]
	public string worldBLayerName = "WorldBOnly";
	private int _worldBLayer = -1;




	void Start()
	{
		CheckWorldSetting();
		
		// Enter World A by default 
		SwitchToWorldA();

		// All "Wolrd B ony" Layer's Object is non-interactivable 
		WorldB_Graph_InteractionUtil.SetInteractive(worldBOnlyRoot, interactive: false);

	}

	// Update is called once per frame
	void Update()
	{

	}



	public void SwitchToWorldA()
	{
		_curWorld = worldA;
		_curWorld.ApplyWorldSettings();
	}

	public void SwitchToWorldB()
	{
		_curWorld = worldB;
		_curWorld.ApplyWorldSettings();
	}

	private void CheckWorldSetting()
	{
		if (!worldBOnlyRoot || !worldA || !worldB)
        {
            Debug.LogWarning("WorldManager: you haven't setting world root yet");
            return;
        }
	}	


}
