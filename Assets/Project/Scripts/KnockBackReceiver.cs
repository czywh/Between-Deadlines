using UnityEngine;

[DisallowMultipleComponent]
public class KnockbackReceiver : MonoBehaviour
{
    [Header("Tuning")]
    public float decayPerSecond = 6f;
    public float upFactor = 0.15f;
    public bool ignoreYFromSource = true;
    
    [Range(0f, 0.95f)] public float resistance = 0f;

    
    [Header("Driver")]
    public bool driveCharacterController = false;

    private Vector3 knockVel;   
    private CharacterController cc;
    private Rigidbody rb;

    public bool IsActive => knockVel.sqrMagnitude > 0.0001f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
    }

    
    public void AddKnockback(Vector3 sourcePosition, float force, float? overrideUpFactor = null)
    {
        var dir = (transform.position - sourcePosition);
        if (ignoreYFromSource) dir.y = 0f;
        dir = dir.sqrMagnitude < 0.0001f ? transform.forward : dir.normalized;

        float up = (overrideUpFactor ?? upFactor);
        Vector3 impulse = dir * force * (1f - resistance) + Vector3.up * force * up;
        knockVel += impulse; 
    }

  
    public void AddKnockbackDir(Vector3 worldDirNormalized, float force, float? overrideUpFactor = null)
    {
        Vector3 dir = worldDirNormalized.sqrMagnitude < 0.0001f ? transform.forward : worldDirNormalized.normalized;
        float up = (overrideUpFactor ?? upFactor);
        Vector3 impulse = dir * force * (1f - resistance) + Vector3.up * force * up;
        knockVel += impulse;
    }

    
    public Vector3 ConsumeDelta(float dt)
    {
       
        Vector3 delta = knockVel * dt;

       
        float k = Mathf.Exp(-decayPerSecond * dt);
        knockVel *= k;

        
        if (knockVel.sqrMagnitude < 1e-4f) knockVel = Vector3.zero;

        return delta;
    }

    void Update()
    {
       
        if (driveCharacterController && cc != null)
        {
            Vector3 delta = ConsumeDelta(Time.deltaTime);
            if (delta.sqrMagnitude > 0f)
                cc.Move(delta);
        }
    }
}