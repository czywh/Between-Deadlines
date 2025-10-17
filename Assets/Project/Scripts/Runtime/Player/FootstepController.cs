using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepController : MonoBehaviour
{
    // --- AUDIO SETTINGS ---
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;

    // --- TIMING SETTINGS ---
    [Header("Timing")]
    public float baseStepInterval = 0.45f;
    public float minStepInterval = 0.24f;
    public float moveThreshold = 0.2f;
    public float expectedMaxSpeed = 6f;

    private CharacterController controller;
    private float stepTimer;
    private int lastClipIndex = -1;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // ensure distances are valid
        if (audioSource.minDistance < 0.3f) audioSource.minDistance = 0.8f;
        if (audioSource.maxDistance < 20f) audioSource.maxDistance = 50f;
    }

    void Update()
    {
        float speed = controller.velocity.magnitude;
        bool grounded = IsGroundedStable();

        // check if player is actually pressing movement keys
        float inputMagnitude = Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical"));
        bool hasInput = inputMagnitude > 0.1f;

        bool moving = (speed > moveThreshold || hasInput) && grounded;

        if (grounded && moving)
        {
            float t = Mathf.InverseLerp(0f, expectedMaxSpeed, speed);
            float dynamicInterval = Mathf.Lerp(baseStepInterval, minStepInterval, t);

            stepTimer += Time.deltaTime;

            if (stepTimer >= dynamicInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    // grounded detection using raycast
    bool IsGroundedStable()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        bool grounded = Physics.Raycast(origin, Vector3.down, out RaycastHit _, 0.3f);

        return grounded;
    }

    void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        int index = Random.Range(0, footstepClips.Length);
        if (index == lastClipIndex && footstepClips.Length > 1)
            index = (index + 1) % footstepClips.Length;
        lastClipIndex = index;

        audioSource.pitch = Random.Range(0.96f, 1.04f);
        audioSource.PlayOneShot(footstepClips[index]);
    }
}
