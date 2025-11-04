using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class ClickCycleMover_Curve : MonoBehaviour
{
   
    public Transform[] points;

    [Header("Movement Settings")]
    
    public float moveDuration = 1.5f;
   
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float clickCooldown = 0.8f;
   
    public bool loop = true;

    public bool snapToFirstPointOnStart = true;

    private int _currentIndex = 0;
    private bool _isMoving = false;
    private float _lastClickTime = -Mathf.Infinity;
    private Coroutine _moveCo;

    void Start()
    {
        if (points == null || points.Length == 0)
        {
            
            enabled = false;
            return;
        }

        if (snapToFirstPointOnStart && points[0] != null)
        {
            transform.position = points[0].position;
            _currentIndex = 0;
        }
        else
        {
            _currentIndex = GetNearestIndex(transform.position);
        }
    }

    void OnMouseDown()
    {
       
        if (Time.time - _lastClickTime < clickCooldown) return;
        if (_isMoving) return;

        int nextIndex = _currentIndex + 1;
        if (nextIndex >= points.Length)
        {
            if (loop) nextIndex = 0;
            else return; 
        }

        _lastClickTime = Time.time;
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(MoveToPoint(points[nextIndex]));
        _currentIndex = nextIndex;
    }

    private IEnumerator MoveToPoint(Transform target)
    {
        if (target == null) yield break;

        _isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = target.position;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float curveT = moveCurve.Evaluate(t); 
            transform.position = Vector3.Lerp(startPos, endPos, curveT);
            yield return null;
        }

        transform.position = endPos;
        _isMoving = false;
        _moveCo = null;
    }

    private int GetNearestIndex(Vector3 pos)
    {
        int best = 0;
        float bestDist = float.PositiveInfinity;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            float d = (points[i].position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (points == null || points.Length == 0) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            Gizmos.DrawSphere(points[i].position, 0.12f);
            var next = points[(i + 1) % points.Length];
            if (next != null && (loop || i < points.Length - 1))
                Gizmos.DrawLine(points[i].position, next.position);
        }
    }
#endif
}
