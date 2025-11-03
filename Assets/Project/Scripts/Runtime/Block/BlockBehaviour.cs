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

    public Vector3 Delta { get; private set; } = Vector3.zero;
    private Vector3 _lastPos;

    // step state
    // 0 = untouched, 1 = activated, 2 = broken
    private int steps = 0;
    public bool IsActivated => steps >= 1;
    public bool IsBroken => steps >= 2;

    private Color baseColor;
    private Material originalMaterial;

    private GameObject currentFire;
    private bool firstJumpDone = false;

    void Awake()
    {
        if (!rend) rend = GetComponentInChildren<Renderer>();
        if (rend)
        {
            originalMaterial = rend.material;
            baseColor = originalMaterial.HasProperty("_BaseColor")
                ? originalMaterial.GetColor("_BaseColor")
                : (originalMaterial.HasProperty("_Color") ? originalMaterial.GetColor("_Color") : rend.material.color);
        }

        // tint explodable blocks 
        if (explodeOnLanding && rend)
        {
            if (rend.material.HasProperty("_BaseColor"))
                rend.material.SetColor("_BaseColor", greyTint);
            else if (rend.material.HasProperty("_Color"))
                rend.material.SetColor("_Color", greyTint);
            else
                rend.material.color = greyTint;
        }
    }

    void Start()
    {
        startPos = transform.position;
        floatSpeed = Random.Range(1.5f, 2.5f);
        floatAmplitude = Random.Range(0.1f, 0.3f);
        _lastPos = transform.position;

        // for explodable blocks, keep fire effect active from start
        if (explodeOnLanding)
            EnsureFireLoop();
    }

    void Update()
    {
        /* uncomment to enable floating motion
        if (canFloat && !IsBroken)
        {
            float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = startPos + new Vector3(0, offset, 0);
        }
        */

        Delta = transform.position - _lastPos;
        _lastPos = transform.position;
    }

    public void OnPortalActive(bool active)
    {
        canFloat = active;
        startPos = transform.position;
        _lastPos = transform.position;
        Delta = Vector3.zero;

        if (!active)
        {
            // reset state and appearance when deactivated
            steps = 0;
            firstJumpDone = false;

            if (rend)
            {
                if (originalMaterial != null) rend.material = originalMaterial;

                if (rend.material.HasProperty("_BaseColor"))
                    rend.material.SetColor("_BaseColor", baseColor);
                else if (rend.material.HasProperty("_Color"))
                    rend.material.SetColor("_Color", baseColor);
                else
                    rend.material.color = baseColor;

                rend.enabled = true;
            }

            if (currentFire != null)
            {
                Destroy(currentFire);
                currentFire = null;
            }

            var col = GetComponent<Collider>();
            if (col) col.enabled = true;
        }
        else
        {
            // restore fire effect for explodable blocks on activation
            if (explodeOnLanding)
                EnsureFireLoop();
        }
    }

    public void ApplyFirstStepIfNeeded()
    {
        // explodable blocks skip first step logic
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

    }

    public void ApplySecondStepIfReady()
    {
        if (IsBroken || !IsActivated) return; // only after first step
        BreakNow();
    }

    public void BreakNow()
    {
        Debug.Log($"[Block] BreakNow CALLED steps(before)={steps} IsBroken={IsBroken}", this);

        if (IsBroken) return;
        steps = 2;

        // stop fire effect
        if (currentFire != null)
        {
            Destroy(currentFire);
            currentFire = null;
        }

        // play explosion or collision effect 
        if (fireExplosionPrefab != null)
        {
            Instantiate(fireExplosionPrefab, transform.position, Quaternion.identity);
        }
        else if (collisionParticles)
        {
            var fx = Instantiate(collisionParticles, transform.position, Quaternion.identity);
            var main = fx.main;
            if (rend)
            {
                Color useColor = baseColor;
                if (rend.material.HasProperty("_BaseColor")) useColor = rend.material.GetColor("_BaseColor");
                else if (rend.material.HasProperty("_Color")) useColor = rend.material.GetColor("_Color");
                main.startColor = useColor;
            }
            Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.25f);
        }

        // disable collision and rendering 
        foreach (var c in GetComponentsInChildren<Collider>(true))  c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true))  r.enabled = false;

        Destroy(gameObject, 1f);
    }

    private void EnsureFireLoop()
    {
        if (fireParticlePrefab != null && currentFire == null)
        {
            currentFire = Instantiate(
                fireParticlePrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity,
                transform
            );
        }
    }
}