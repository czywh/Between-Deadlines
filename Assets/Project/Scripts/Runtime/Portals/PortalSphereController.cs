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
    public float maxRadius = 3f;    // Max radius of portal sphere
    public float expandSpeed = 3f;  // Expanding speed
    public float duration = 10f;    // Sustain duration
    public float cooldown = 2f;     // Cooldown for re-open

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

    public MusicManager musicManager;

    // ===== Lifecycle parameters & events =====
    public enum State { Idle, Expanding, Sustaining, Collapsing, Cooldown }
    private State _state = State.Idle;

    // External observation
    public State CurrentState => _state;
    public bool IsSustaining => _state == State.Sustaining;
    public System.Action<State> OnStateChanged;

    private void SetState(State s)
    {
        if (_state == s) return;
        var prev = _state;
        _state = s;

        // Fire edge events around Sustaining
        if (s == State.Sustaining && prev != State.Sustaining)
        {
            try { OnSustainingStarted?.Invoke(); } catch { }
        }
        if (prev == State.Sustaining && s != State.Sustaining)
        {
            try { OnSustainingEnded?.Invoke(); } catch { }
        }
        try { OnStateChanged?.Invoke(_state); } catch { }
    }
    public UnityEngine.Events.UnityEvent OnSustainingStarted;
    public UnityEngine.Events.UnityEvent OnSustainingEnded;

    private Coroutine _co;

    // Active World B objects under trigger
    private readonly HashSet<Transform> _activeB = new();
    private int _worldBOnlyLayer = -1;

    private SphereCollider _triggerCollider;
    private float _radius;
    private bool _worldBisActive;
    private bool _cameraInsideSphere;

    // Shader globals
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
            Debug.LogError($"UnSetting Layer '{worldBOnlyLayerName}', please add it in Project Settings → Tags and Layers.");

        // Camera default
        if (!mainCamera) mainCamera = Camera.main;
        if (portalCamera)
        {
            portalCamera.enabled = false;
            portalCamera.cullingMask = portalCameraCulling;
        }
    }

    void Start()
    {
        _triggerCollider.enabled = false;
        _cameraInsideSphere = false;

        SetRadius(0f);
        SetPortalVisible(false);
        SetWorldColorMode(false);

        // Initial shader globals
        Shader.SetGlobalVector(ShaderID_Center, transform.position);
        Shader.SetGlobalFloat(ShaderID_Radius, 0f);
        Shader.SetGlobalFloat("_UsePortalDistanceFade", useDistanceFade ? 1f : 0f);
    }

    void Update()
    {
        // Toggle lifecycle
        if (Input.GetKeyDown(toggleKey)) HandleToggle();

        // Follow player
        if (player) transform.position = player.position;

        // Update shader globals
        Shader.SetGlobalFloat("_UsePortalDistanceFade", useDistanceFade ? 1f : 0f);
        Shader.SetGlobalVector(ShaderID_Center, transform.position);
        Shader.SetGlobalFloat(ShaderID_Radius, _radius);
    }

    void LateUpdate()
    {
        if (!_worldBisActive) return;

        bool inside = CheckCameraInsideSphere();

        // 1) Gray / Color volumes driven by inside flag
        SetWorldColorMode(inside);

        // 2) ★ Always use RT path (no Clip), guarantees shader RT sampling & shake
        var ctx = BuildRenderPathContext();
        _currentPath = WorldB_Graph_InteractionUtil.UpdatePath_AlwaysRT(ref portalRT, in ctx); // ★

        // 3) Keep materials in RT mode and bind RT to sphere visuals
        WorldB_Graph_InteractionUtil.ApplyMaterialPath(worldBOnlyRoot, _worldBOnlyLayer, _currentPath);
        WorldB_Graph_InteractionUtil.BindRTToSphereMaterials(sphereVisuals, portalRT, sphereRTProperty, keepWhenNull: true);
    }

    private float GetRadius() => transform.localScale.x * 0.5f;

    private void SetRadius(float radius)
    {
        _radius = radius;
        float d = Mathf.Max(radius * 2f, 0f);
        transform.localScale = new Vector3(d, d, d);

        // Fix trigger collider (avoid double scaling)
        _triggerCollider.radius = radius;
    }

    private void SetPortalVisible(bool visible)
    {
        if (sphereVisuals == null) return;
        foreach (var mr in sphereVisuals)
            if (mr) mr.enabled = visible;
    }

    private void HandleToggle()
    {
        switch (_state)
        {
            case State.Idle:
                StartLifecycle();
                break;

            case State.Expanding:
            case State.Sustaining:
                EarlyCollapse();
                break;

            default:
                break;
        }
    }

    private void StartLifecycle()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoroutineLifecycle());
    }

    private IEnumerator CoroutineLifecycle()
    {
        // 1) Expand
        SetState(State.Expanding);
        WorldB_ActiveStart();
        yield return CoroutineRadius(0f, maxRadius, expandSpeed);

        // 2) Sustain
        SetState(State.Sustaining);
        yield return new WaitForSeconds(duration);

        // 3) Collapse
        SetState(State.Collapsing);
        yield return CoroutineRadius(GetRadius(), 0f, expandSpeed);

        WorldB_ActiveEnd();

        // 4) Cooldown
        SetState(State.Cooldown);
        yield return new WaitForSeconds(cooldown);

        // 5) Idle
        SetState(State.Idle);
        _co = null;
    }

    private void EarlyCollapse()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(EarlyCollapseCoroutine());
    }

    private IEnumerator EarlyCollapseCoroutine()
    {
        // 1) Collapse
        SetState(State.Collapsing);
        yield return CoroutineRadius(GetRadius(), 0f, expandSpeed);

        WorldB_ActiveEnd();

        // 2) Cooldown
        SetState(State.Cooldown);
        yield return new WaitForSeconds(cooldown);

        // 3) Idle
        SetState(State.Idle);
        _co = null;
    }

    private IEnumerator CoroutineRadius(float from, float to, float speed)
    {
        float cur = from;
        while (!Mathf.Approximately(cur, to))
        {
            cur = Mathf.MoveTowards(cur, to, speed * Time.deltaTime);
            SetRadius(cur);
            yield return null;
        }
        SetRadius(to);
    }

    private void WorldB_ActiveStart()
    {
        SetPortalVisible(true);

        if (musicManager != null)
            musicManager.OnSphereOpened();

        _worldBisActive = true;

        if (_triggerCollider) _triggerCollider.enabled = true;

        _currentPath = RenderPath.RT; // Using Render Texture method by default

        // ★ Always RT path on activation
        var ctx = BuildRenderPathContext();
        _currentPath = WorldB_Graph_InteractionUtil.UpdatePath_AlwaysRT(ref portalRT, in ctx); // ★
        WorldB_Graph_InteractionUtil.EnsureRTBinding(ctx);

        // ★ Optional: immediate material binding to avoid 1-frame delay
        WorldB_Graph_InteractionUtil.ApplyMaterialPath(worldBOnlyRoot, _worldBOnlyLayer, _currentPath);
        WorldB_Graph_InteractionUtil.BindRTToSphereMaterials(sphereVisuals, portalRT, sphereRTProperty, keepWhenNull: true);
    }

    private void WorldB_ActiveEnd()
    {
        SetPortalVisible(false);

        if (musicManager != null)
            musicManager.OnSphereClosed();

        if (portalCamera) portalCamera.enabled = false;

        // Reset interactive flags
        foreach (var i in _activeB)
            if (i) WorldB_Graph_InteractionUtil.SetInteractive(i, false);
        _activeB.Clear();

        if (_triggerCollider) _triggerCollider.enabled = false;

        // Back to World A: release RT & clear bindings
        var ctx = BuildRenderPathContext();
        WorldB_Graph_InteractionUtil.ReleaseRT(ref portalRT, portalCamera);

        if (mainCamera) mainCamera.cullingMask &= ~(1 << _worldBOnlyLayer);
        _worldBisActive = false;
    }

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
            rtResolutionScale = rtResolutionScale
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

    // ★ Release RT on disable to avoid leaks in Play Mode stop/start
    private void OnDisable()
    {
        if (portalRT)
            WorldB_Graph_InteractionUtil.ReleaseRT(ref portalRT, portalCamera);
    }
}