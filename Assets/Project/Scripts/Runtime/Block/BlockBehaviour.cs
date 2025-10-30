using UnityEngine;

public class BlockBehaviour : MonoBehaviour
{

    [Header("Mode")]
    [SerializeField] private bool explodeOnLanding = false;
    public bool ExplodeOnLanding => explodeOnLanding;

    [SerializeField] private Color greyTint = new Color(0.6f, 0.6f, 0.6f);

    [Header("FX")]
    [SerializeField] private ParticleSystem collisionParticles;
    [SerializeField] private Renderer rend;
    [SerializeField] private Material firstJumpMaterial;    
    [SerializeField] private GameObject fireParticlePrefab;
    [SerializeField] private GameObject fireExplosionPrefab;

    [Header("Motion (optional)")]
    [SerializeField] private bool canFloat = true;
    private float floatSpeed;
    private float floatAmplitude;
    private Vector3 startPos;

    // platform carry delta
    public Vector3 Delta { get; private set; } = Vector3.zero;
    private Vector3 _lastPos;

    // step state
    // 0 = untouched, 1 = activated (white), 2 = broken
    private int steps = 0;
    public bool IsActivated => steps >= 1;
    public bool IsBroken => steps >= 2;

    private Color baseColor;

    private GameObject currentFire;
    private bool firstJumpDone = false;

    void Awake()
    {
        if (!rend) rend = GetComponentInChildren<Renderer>();
        if (rend) baseColor = rend.material.color;

        // ---------  Visual cue in editor: grey blocks = instant explode ones
        if (explodeOnLanding && rend)
            rend.material.color = greyTint;
    }

    void Start()
    {
        startPos = transform.position;
        floatSpeed = Random.Range(1.5f, 2.5f);
        floatAmplitude = Random.Range(0.1f, 0.3f);
        _lastPos = transform.position;
    }

    void Update()
    {
        /* if (canFloat && !IsBroken)
        {
            float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = startPos + new Vector3(0, offset, 0);
        } */

        Delta = transform.position - _lastPos;
        _lastPos = transform.position;
    }

    public void OnPortalActive(bool active)
    {
        // pause/resume floating and re-anchor so it doesn't snap
        canFloat = active;
        startPos = transform.position;
        _lastPos = transform.position;
        Delta = Vector3.zero;

        if (!active)
        {
            // reset visuals & state when turning OFF the portal/platform
            if (rend) rend.material.color = baseColor;
            steps = 0;

            var col = GetComponent<Collider>();
            if (col) col.enabled = true;
            if (rend) rend.enabled = true;
        }
    }

    public void ApplyFirstStepIfNeeded()
    {
        Debug.Log($"[Block] FirstStep? explodeOnLanding={explodeOnLanding} steps(before)={steps}", this);

        if (explodeOnLanding) return;

        if (IsBroken || IsActivated) return;
        steps = 1;
        if (rend && firstJumpMaterial != null)
            rend.material = firstJumpMaterial;

        if (!firstJumpDone && fireParticlePrefab != null)
        {
            currentFire = Instantiate(fireParticlePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity, transform);
            firstJumpDone = true;
        }

        Debug.Log($"[Block] FirstStep APPLIED steps(now)={steps}", this);
    }

    public void ApplySecondStepIfReady()
    {
        if (IsBroken || !IsActivated) return; // only after first step
        BreakNow();
    }

    public void BreakNow()   // must be public
    {
        Debug.Log($"[Block] BreakNow CALLED steps(before)={steps} IsBroken={IsBroken}", this);

        if (IsBroken) return;
        steps = 2;

        if (currentFire != null)
        {
            Destroy(currentFire);
            currentFire = null;
        }

        if (fireExplosionPrefab != null)
        {
            Instantiate(fireExplosionPrefab, transform.position, Quaternion.identity);
        }
        else if (collisionParticles)
        {
            var fx = Instantiate(collisionParticles, transform.position, Quaternion.identity);
            var main = fx.main;
            if (rend) main.startColor = rend.material.color;
            Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.25f);
        }

        foreach (var c in GetComponentsInChildren<Collider>(true))  c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true))  r.enabled = false;

        Destroy(gameObject, 1f);
    }
}
