using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class BossShooter : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxHP = 5;
    public float fireInterval = 1.2f;

    [Header("References")]
    public Transform player;
    public Transform firePoint;
    public GameObject bulletPrefab;

    [Header("Hit Feedback")]
    public GameObject hitEffectPrefab;
    public Renderer[] flashRenderers;
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f;

    [Header("Death Effect (no fixed lifetime)")]
    public GameObject deathEffectPrefab;

    [Header("Hit Shake")]
    public float shakeIntensity = 0.2f;
    public float shakeDuration = 0.15f;

    [Header("Aggro / Activation")]
    public float activateRange = 12f;
    public float deactivateRange = 14f;
    public bool requireLineOfSight = false;

    public LayerMask losObstacles = ~0;

    [Header("Aiming / Rotation")]
    public bool facePlayer = true;
    public bool yawOnly = true;
    public float turnSpeed = 360f;
    public bool rotateOnlyWhenAggro = true;

    private int _hp;
    private Coroutine _fireCo;
    private Collider _bossCollider;

    private MaterialPropertyBlock _mpb;
    private static readonly int _ColorPropBase = Shader.PropertyToID("_BaseColor");
    private static readonly int _ColorPropStd = Shader.PropertyToID("_Color");
    private Color[] _origColors;

    private bool _aggro = false;

    public int CurrentHP => _hp;
    public int MaxHP => maxHP;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color activateColor = new Color(0f, 1f, 0.6f, 1f);
    public Color deactivateColor = new Color(1f, 0.7f, 0f, 1f);
    public bool drawWhenUnselected = false;

    void Awake()
    {
        _hp = maxHP;
        _bossCollider = GetComponent<Collider>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        _mpb = new MaterialPropertyBlock();

        if (flashRenderers != null && flashRenderers.Length > 0)
        {
            _origColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                var r = flashRenderers[i];
                if (!r) { _origColors[i] = Color.white; continue; }

                r.GetPropertyBlock(_mpb);
                if (_mpb != null && (_mpb.GetVector(_ColorPropBase) != Vector4.zero))
                    _origColors[i] = _mpb.GetColor(_ColorPropBase);
                else if (_mpb != null && (_mpb.GetVector(_ColorPropStd) != Vector4.zero))
                    _origColors[i] = _mpb.GetColor(_ColorPropStd);
                else if (r.sharedMaterial != null)
                {
                    if (r.sharedMaterial.HasProperty(_ColorPropBase))
                        _origColors[i] = r.sharedMaterial.GetColor(_ColorPropBase);
                    else if (r.sharedMaterial.HasProperty(_ColorPropStd))
                        _origColors[i] = r.sharedMaterial.GetColor(_ColorPropStd);
                    else
                        _origColors[i] = Color.white;
                }
                else _origColors[i] = Color.white;
            }
        }
    }

    void OnEnable()
    {
        if (_fireCo == null) _fireCo = StartCoroutine(FireLoop());
    }

    void OnDisable()
    {
        if (_fireCo != null)
        {
            StopCoroutine(_fireCo);
            _fireCo = null;
        }
    }

    void Update()
    {
        UpdateAggro();
        UpdateAimRotation();
    }

    IEnumerator FireLoop()
    {
        var wait = new WaitForSeconds(fireInterval);
        while (true)
        {
            yield return wait;
            if (_aggro)
                TryFire();
        }
    }

    void TryFire()
    {
        if (player == null || bulletPrefab == null || firePoint == null) return;

        Vector3 dir = (player.position - firePoint.position).normalized;
        var go = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir, Vector3.up));

        var bullet = go.GetComponent<ReflectableBullet>();
        if (bullet != null)
        {
            bullet.Init(
                initialDir: dir,
                ownerToIgnore: _bossCollider,
                ownerIgnoreTime: 0.2f
            );
        }
    }

    public void TakeDamage(int amount, Vector3 hitPoint)
    {
        _hp -= Mathf.Max(1, amount);
        _hp = Mathf.Max(0, _hp);

        if (hitEffectPrefab)
        {
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        }

        if (flashRenderers != null && flashRenderers.Length > 0)
            StartCoroutine(FlashOnce());

        StartCoroutine(ShakeOnce());

        if (_hp <= 0) Die();
    }

    public void TakeDamage(int amount) => TakeDamage(amount, transform.position + Vector3.up * 1f);

    private void Die()
    {
        if (_fireCo != null)
        {
            StopCoroutine(_fireCo);
            _fireCo = null;
        }

        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject, 0.25f);
    }

    private IEnumerator FlashOnce()
    {
        if (flashRenderers == null || flashRenderers.Length == 0) yield break;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            var r = flashRenderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(_ColorPropBase, flashColor);
            r.SetPropertyBlock(_mpb);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            var r = flashRenderers[i];
            if (!r) continue;

            var restore = (_origColors != null && i < _origColors.Length) ? _origColors[i] : Color.white;
            r.GetPropertyBlock(_mpb);
            if (_mpb != null)
            {
                _mpb.SetColor(_ColorPropBase, restore);
                _mpb.SetColor(_ColorPropStd, restore);
                r.SetPropertyBlock(_mpb);
            }
        }
    }

    private IEnumerator ShakeOnce()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetZ = Random.Range(-1f, 1f) * shakeIntensity * 0.5f;
            transform.position = originalPos + new Vector3(offsetX, offsetY, offsetZ);
            yield return null;
        }

        transform.position = originalPos;
    }

    private void UpdateAggro()
    {
        if (player == null)
        {
            _aggro = false;
            return;
        }

        float d = Vector3.Distance(transform.position, player.position);

        if (_aggro)
        {
            if (d > deactivateRange || !HasLineOfSight())
                _aggro = false;
        }
        else
        {
            if (d <= activateRange && HasLineOfSight())
                _aggro = true;
        }
    }

    private bool HasLineOfSight()
    {
        if (!requireLineOfSight) return true;
        if (player == null) return false;

        Vector3 origin = firePoint ? firePoint.position : transform.position;
        Vector3 toPlayer = player.position - origin;
        float dist = toPlayer.magnitude;
        if (dist <= 0.001f) return true;

        Vector3 dir = toPlayer / Mathf.Max(dist, 0.0001f);
        return !Physics.Raycast(origin, dir, dist, losObstacles, QueryTriggerInteraction.Ignore);
    }

    private void UpdateAimRotation()
{
    if (!facePlayer) return;
    if (player == null) return;
    if (rotateOnlyWhenAggro && !_aggro) return;

    // Direction on horizontal plane only
    Vector3 toPlayer = player.position - transform.position;
    toPlayer.y = 0f;
    if (toPlayer.sqrMagnitude < 0.0001f) return;

    // Base horizontal rotation toward player
    Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);

    // Add a fixed 90° tilt on the X-axis
    Quaternion tilt = Quaternion.Euler(-90f, 0f, 90f);
    targetRot = targetRot * tilt;

    // Smoothly rotate toward that orientation
    transform.rotation = Quaternion.RotateTowards(
        transform.rotation,
        targetRot,
        turnSpeed * Time.deltaTime
    );
}
    void OnDrawGizmos()
    {
        if (!drawWhenUnselected || !showGizmos) return;
        DrawRangesGizmo();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        DrawRangesGizmo();
    }

    private void DrawRangesGizmo()
    {
        Vector3 p = transform.position;
        Gizmos.color = activateColor;
        Gizmos.DrawWireSphere(p, Mathf.Max(0f, activateRange));

        Gizmos.color = deactivateColor;
        Gizmos.DrawWireSphere(p, Mathf.Max(0f, deactivateRange));

        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(p, player.position);
            Gizmos.DrawWireSphere(player.position, 0.2f);
        }
    }

#if UNITY_EDITOR
    private void OnSceneGUI() { }
#endif
}