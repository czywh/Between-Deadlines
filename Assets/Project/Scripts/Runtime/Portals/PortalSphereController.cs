using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;



public class PortalSphereController : MonoBehaviour
{
	[Header("References")]
    public Transform player;
    public Camera mainCamera;
    public Camera portalCamera;
    public Renderer[] sphereVisuals;  
    public Transform worldBOnlyRoot;   


	[Header("Portal Lifecycle")]
	public KeyCode toggleKey = KeyCode.Q;
	public float maxRadius = 3f;   // MaxRadius of portal sphere
	public float expandSpeed = 3f;   // Expanding speed of portal 
	public float duration = 10f;   // sustain duration of portal
	public float cooldown = 2f;   // coold down for portal re-open

	[Header("World & Layer Settings")]
	public string worldBOnlyLayerName = "World B only";
	public LayerMask portalCameraCulling;
	public bool useDistanceFade = true;


	[Header("RenderTexture Settings")]
	public bool useRenderTexture = true;
    public RenderTexture portalRT;
    public Material sphereMaterialForRT;
    public string sphereRTProperty = "_PortalRT";
    [Range(0.25f, 1f)] public float rtResolutionScale = 1f;
    [SerializeField] private RenderPath _currentPath = RenderPath.RT;



	[Header("Post-Processing Volumes")]
    public Volume volumeGray;
    public Volume volumeColor;


	// Lifecycle paramerters 
	private enum State { Idle, Expanding, Sustaining, Collapsing, Cooldown }
    private State _state = State.Idle;
    private Coroutine _co;

	// Relavent touched Wolrd B only object
	private readonly HashSet<Transform> _activeB = new();
    private int _worldBOnlyLayer = -1;


	private SphereCollider _triggerCollider;
	private float _radius;
	private bool _worldBisActive;
	private bool _cameraInsideSphere;


	// Shader parameters 
	private static readonly int ShaderID_Center = Shader.PropertyToID("_PortalCenter");
	private static readonly int ShaderID_Radius = Shader.PropertyToID("_PortalRadius");


	void Awake()
	{
		_worldBisActive = false;

		if (!TryGetComponent<Rigidbody>(out var rb))
		{
			rb = gameObject.AddComponent<Rigidbody>();
			rb.isKinematic = true;
			rb.useGravity = false;
		}

		if (!TryGetComponent<SphereCollider>(out _triggerCollider))
			_triggerCollider = gameObject.AddComponent<SphereCollider>();
			_triggerCollider.isTrigger = true;

		_worldBOnlyLayer = LayerMask.NameToLayer(worldBOnlyLayerName);
		if (_worldBOnlyLayer == -1)
			Debug.LogError($"UnSetting Layer '{worldBOnlyLayerName}', Adding 'Tags and Layer' in project setting");


		// Camera Setting: Portal Camera is cloased initially and only render WorldB
		if (!mainCamera) mainCamera = Camera.main;
		if (portalCamera)
		{
			portalCamera.enabled = false;
			portalCamera.cullingMask = portalCameraCulling;
		}
	}

	void Start()
	{
		// at the start of the game, portal is closed 
		_triggerCollider.enabled = false;
		_cameraInsideSphere = false;

		SetRadius(0f);
		SetPortalVisible(false);
		SetWorldColorMode(false);
		

		// updating portal's postion and radius  for shader 
		Shader.SetGlobalVector(ShaderID_Center, transform.position);
		Shader.SetGlobalFloat(ShaderID_Radius, 0f);
		Shader.SetGlobalFloat("_UsePortalDistanceFade", useDistanceFade ? 1f : 0f);
	}

	// Update is called once per frame
	void Update()
	{
		// Player toggle "Q"
		if (Input.GetKeyDown(toggleKey)) HandleToggle();

		// keep following with player 
		if (player) transform.position = player.position;

		// updating portal's postion and radius for shader 
		Shader.SetGlobalFloat("_UsePortalDistanceFade", useDistanceFade ? 1f : 0f);
		Shader.SetGlobalVector(ShaderID_Center, transform.position);
		Shader.SetGlobalFloat(ShaderID_Radius, _radius);
	}


	void LateUpdate()
	{
		if (!_worldBisActive) return;

		bool inside = CheckCameraInsideSphere();

		// 1. Set Color Mode (Gray / Colorful)
		SetWorldColorMode(inside);

		// 2. Change Camera/ Rendering path 
		var prevPath = _currentPath;
		var ctx = BuildRenderPathContext();
		_currentPath = WorldB_Graph_InteractionUtil.UpdatePathByCameraInside(inside, _currentPath, ref portalRT, in ctx);

		// 3. Updating Render Texture 
		if (_currentPath != prevPath)
        {
            WorldB_Graph_InteractionUtil.ApplyMaterialPath(worldBOnlyRoot, _worldBOnlyLayer, _currentPath);

            if (_currentPath == RenderPath.RT) {
                WorldB_Graph_InteractionUtil.CheckOrRecreateRT(ref portalRT, portalCamera, useRenderTexture, rtResolutionScale, sphereMaterialForRT, sphereRTProperty);

                WorldB_Graph_InteractionUtil.BindRTToSphereMaterials( sphereVisuals, portalRT, sphereRTProperty, keepWhenNull: true);
            }
            else
            {
                WorldB_Graph_InteractionUtil.ClearRTBinding(portalCamera, sphereMaterialForRT, sphereRTProperty, clearMat: false);
                WorldB_Graph_InteractionUtil.BindRTToSphereMaterials( sphereVisuals, null, sphereRTProperty, keepWhenNull: false);
            }
        }	
	}



	private float GetRadius() => transform.localScale.x * 0.5f;

	private void SetRadius(float radius)
	{
		_radius = radius;
		float d = Mathf.Max(radius * 2f, 0f);
		transform.localScale = new Vector3(d, d, d);

		// fix trigger collier radius to aviod double scaling
		_triggerCollider.radius = radius;
	}

	/*
	* Set Visibility of Portal Sphere 
	*/
	private void SetPortalVisible(bool visible)
	{
		if (sphereVisuals == null) return;

		foreach (var meshrender in sphereVisuals)
			if (meshrender) meshrender.enabled = visible;
	}



	private void HandleToggle()
	{
		switch (_state)
		{
			case State.Idle:
				// normal start of sphere life cycle 
				StartLifecycle();
				break;

			case State.Expanding:
			case State.Sustaining:
				// player can early stop sphere spawning 
				EarlyCollapse();
				break;

			// Collapsing, Cooldown：Nothing happened here 
			default:
				break;
		}
	}

	private void StartLifecycle()
	{
		// Stop the current coroutine to avoid coroutine conflicts
		if (_co != null) StopCoroutine(_co);
		_co = StartCoroutine(CoroutineLifecycle());
	}


	/*
	* Normal coroutine Lifecycle of Portal Sphere 
	* Idel -> Exapnd -> Sustain -> Collaps -> Cooldown -> Idel 
	*/
	private IEnumerator CoroutineLifecycle()
	{
		// 1. Expand
		_state = State.Expanding;
		WorldB_ActiveStart();
		yield return CoroutineRadius(0f, maxRadius, expandSpeed);

		// 2. Sustain
		_state = State.Sustaining;
		yield return new WaitForSeconds(duration);


		// 3. Collapse
		_state = State.Collapsing;
		yield return CoroutineRadius(GetRadius(), 0f, expandSpeed);

		WorldB_ActiveEnd();

		// 4. Cooldown
		_state = State.Cooldown;
		yield return new WaitForSeconds(cooldown);

		// 5. Idel 
		_state = State.Idle;
		_co = null;
	}


	private void EarlyCollapse()
	{
		// Stop the current coroutine to avoid coroutine conflicts
		if (_co != null) StopCoroutine(_co);

		// start sphere collasing coroutine 
		_co = StartCoroutine(EarlyCollapseCoroutine());
	}


	/*
	* Early Collaapsing coroutine Lifecycle of Portal Sphere 
	* Exapnd / Sustain -> Collaps -> Cooldown -> Idel 
	*/
	private IEnumerator EarlyCollapseCoroutine()
	{
		// 1. Collaps 
		_state = State.Collapsing;
		yield return CoroutineRadius(GetRadius(), 0f, expandSpeed);

		WorldB_ActiveEnd();

		// 2. Cooldown 
		_state = State.Cooldown;
		yield return new WaitForSeconds(cooldown);


		// 3. Idel
		_state = State.Idle;
		_co = null;
	}

	/*
	* Portal sphere radius expanding/ collapsomg process 
	* For each delta time, the radius will increase / decrease with corresponding speed 	
	*/
	private IEnumerator CoroutineRadius(float from, float to, float speed)
	{
		float cur_radius = from;

		// the radius of Portal Sphere will gradually increase/ decrease to Target number 
		while (!Mathf.Approximately(cur_radius, to))
		{
			cur_radius = Mathf.MoveTowards(cur_radius, to, speed * Time.deltaTime);
			SetRadius(cur_radius);
			yield return null;
		}

		SetRadius(to);
	}


	private void WorldB_ActiveStart()
	{
		SetPortalVisible(true);
		_worldBisActive = true;

		if (_triggerCollider) _triggerCollider.enabled = true;

		_currentPath = RenderPath.RT;       // Using Render Texture method by default 

		var ctx = BuildRenderPathContext();
		_currentPath = WorldB_Graph_InteractionUtil.UpdatePathByCameraInside(CheckCameraInsideSphere(), _currentPath, ref portalRT, in ctx);
		WorldB_Graph_InteractionUtil.EnsureRTBinding(ctx);	
	}

	private void WorldB_ActiveEnd()
	{
		SetPortalVisible(false);
		if (portalCamera) portalCamera.enabled = false;

		// set all actived objects as non-interatable 
		foreach (var i in _activeB)
			if (i) WorldB_Graph_InteractionUtil.SetInteractive(i, false);
		_activeB.Clear();

		if (_triggerCollider) _triggerCollider.enabled = false;


		// Back To World A, close portal Camera, disable "World B Layer" for main camera
		var ctx = BuildRenderPathContext();
		WorldB_Graph_InteractionUtil.ClearRTBinding(portalCamera, sphereMaterialForRT, sphereRTProperty, false);
		WorldB_Graph_InteractionUtil.ReleaseRT(ref portalRT, portalCamera);

		if (mainCamera) mainCamera.cullingMask &= ~(1 << _worldBOnlyLayer);
		_worldBisActive = false;
	}


	/*
	* Check if camera is inside of portal sphere 
	*/
	private bool CheckCameraInsideSphere()
	{
		if (!mainCamera) return false;

		if (!_worldBisActive)
		{
			_cameraInsideSphere = false;
			return false;
		}

		float d = Vector3.Distance(mainCamera.transform.position, transform.position);
		bool inside = d < _radius;

		
		_cameraInsideSphere = inside;
		return inside;
	}

	private RenderPathContext BuildRenderPathContext()
	{
		return new RenderPathContext
		{
			mainCamera = mainCamera,
			portalCamera = portalCamera,
			worldBLayer = _worldBOnlyLayer,
			useRenderTexture = useRenderTexture,
			portalRT = portalRT,
			sphereMaterialForRT = sphereMaterialForRT,
			sphereRTProperty = sphereRTProperty,
			portalCameraCulling = portalCameraCulling,
			useDistanceFade = useDistanceFade,
			rtResolutionScale  = rtResolutionScale 
		};
	}


	private void SetWorldColorMode(bool colorful)
	{
		if (volumeGray) volumeGray.enabled = !colorful;
		if (volumeColor) volumeColor.enabled = colorful;
		Shader.SetGlobalFloat("_PortalColorBlend", colorful ? 1f : 0f);
	}


	private Transform FindTargetLayer(Transform t, int targetLayer)
	{
		while (t != null)
		{
			if (t.gameObject.layer == targetLayer)
				return t;
			t = t.parent;
		}
		return null;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!_worldBisActive) return;

		var bRoot = FindTargetLayer(other.transform, _worldBOnlyLayer);
		if (!bRoot) return;

		_activeB.Add(bRoot);
		WorldB_Graph_InteractionUtil.SetInteractive(bRoot, true);
	}


	private void OnTriggerExit(Collider other)
	{
		if (!_worldBisActive) return;

		var bRoot = FindTargetLayer(other.transform, _worldBOnlyLayer);
		if (!bRoot) return;

		_activeB.Remove(bRoot);
		WorldB_Graph_InteractionUtil.SetInteractive(bRoot, false);
	}

}
