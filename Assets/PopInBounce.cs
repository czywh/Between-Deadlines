using System.Collections;
using UnityEngine;

/// 生成：从下往上+衰减震荡；
/// 销毁：先做短暂震荡，再下沉消失（可回调销毁）
public class PopInBounce : MonoBehaviour
{
    [Header("Rise (上升/下沉幅度)")]
    public float riseDistance = 1.0f;
    public float riseDuration = 0.35f;
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shake (震荡)")]
    public float shakeAmplitude = 0.15f;
    public float shakeFrequency = 10f;
    public float shakeDamping = 4f;
    public float shakeDuration = 0.6f;

    [Header("Scale (缩放可选)")]
    public bool scaleOvershoot = true;
    public float scaleAmount = 0.08f;
    public float scaleDamping = 5f;
    public float scaleFrequency = 8f;

    [Header("Options")]
    public bool useLocal = false;
    public bool autoPlayOnEnable = true;
    public bool temporaryKinematic = true;

    private Vector3 _basePos;
    private Vector3 _targetPos;
    private Vector3 _startScale;
    private Rigidbody _rb;
    private bool _origKinematic;
    private bool _playing;

    private void OnEnable()
    {
        if (autoPlayOnEnable) PlayIn();
    }

    public void PlayIn()
    {
        if (_playing) StopAllCoroutines();
        StartCoroutine(CoIn());
    }

    public void PlayOutAndThen(System.Action onDone)
    {
        if (_playing) StopAllCoroutines();
        StartCoroutine(CoOut(onDone));
    }

    public void PlayOutAndDestroy()
    {
        PlayOutAndThen(() => Destroy(gameObject));
    }

    IEnumerator CoIn()
    {
        _playing = true;

        _rb = GetComponent<Rigidbody>();
        if (_rb && temporaryKinematic) {
            _origKinematic = _rb.isKinematic;
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        _startScale = transform.localScale;

        Vector3 pos = useLocal ? transform.localPosition : transform.position;
        _targetPos = pos;
        _basePos = pos - Vector3.up * riseDistance;

        if (useLocal) transform.localPosition = _basePos;
        else transform.position = _basePos;

        // 上升
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, riseDuration);
            float k = riseCurve.Evaluate(Mathf.Clamp01(t));
            Vector3 p = Vector3.LerpUnclamped(_basePos, _targetPos, k);
            if (useLocal) transform.localPosition = p; else transform.position = p;

            if (scaleOvershoot) {
                float s = 1f + scaleAmount * Mathf.Sin(Mathf.PI * k) * 0.2f;
                transform.localScale = _startScale * s;
            }
            yield return null;
        }

        // 震荡
        float time = 0f;
        while (time < shakeDuration)
        {
            time += Time.deltaTime;
            float decay = Mathf.Exp(-shakeDamping * time);
            float y = shakeAmplitude * decay * Mathf.Sin(2f * Mathf.PI * shakeFrequency * time);

            Vector3 posY = _targetPos + Vector3.up * y;
            if (useLocal) transform.localPosition = posY; else transform.position = posY;

            if (scaleOvershoot) {
                float sc = 1f + scaleAmount * Mathf.Exp(-scaleDamping * time) *
                           Mathf.Sin(2f * Mathf.PI * scaleFrequency * time);
                transform.localScale = _startScale * sc;
            }
            yield return null;
        }

        if (useLocal) transform.localPosition = _targetPos; else transform.position = _targetPos;
        transform.localScale = _startScale;

        if (_rb && temporaryKinematic) _rb.isKinematic = _origKinematic;
        _playing = false;
    }

    IEnumerator CoOut(System.Action onDone)
    {
        _playing = true;

        _rb = GetComponent<Rigidbody>();
        bool restore = false;
        if (_rb && temporaryKinematic) {
            restore = true;
            _origKinematic = _rb.isKinematic;
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        // 目标位置
        Vector3 pos0 = useLocal ? transform.localPosition : transform.position;
        _targetPos = pos0;
        _basePos = pos0 - Vector3.up * riseDistance; // 下沉目标

        // 简短的反向震荡（先上后下，幅度逐渐减小）
        float time = 0f;
        float outDuration = Mathf.Max(0.2f, shakeDuration * 0.5f);
        while (time < outDuration)
        {
            time += Time.deltaTime;
            float k = time / outDuration;
            float decay = Mathf.Exp(-shakeDamping * time);
            float y = shakeAmplitude * decay * Mathf.Sin(2f * Mathf.PI * shakeFrequency * time + Mathf.PI); // 反相

            Vector3 posY = _targetPos + Vector3.up * y;
            if (useLocal) transform.localPosition = posY; else transform.position = posY;

            if (scaleOvershoot) {
                float sc = 1f + (scaleAmount * 0.8f) * Mathf.Exp(-scaleDamping * time) *
                           Mathf.Sin(2f * Mathf.PI * scaleFrequency * time + Mathf.PI);
                transform.localScale = _startScale * sc;
            }
            yield return null;
        }

        // 下沉消失
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, riseDuration);
            float k = riseCurve.Evaluate(Mathf.Clamp01(t));
            Vector3 p = Vector3.LerpUnclamped(_targetPos, _basePos, k);
            if (useLocal) transform.localPosition = p; else transform.position = p;

            if (scaleOvershoot) {
                float s = 1f - scaleAmount * Mathf.Sin(Mathf.PI * k) * 0.2f;
                transform.localScale = _startScale * s;
            }
            yield return null;
        }

        if (restore) _rb.isKinematic = _origKinematic;
        _playing = false;
        onDone?.Invoke();
    }
}