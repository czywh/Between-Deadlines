using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTransform;

    [Header("Movement")]
    public float speed = 3f;
    public float rotationSpeed = 10f;
    public float sprintMultiplier = 2f;

    [Header("Jump & Gravity")]
    public float jumpForce = 1f;
    public float gravity = -20f;

    [Header("Slope & Sliding Settings")]
    public float slopeLimit = 8f;
    public float slideSpeed = 4f;
    public float groundCheckDistance = 1.2f;

    [Header("Ground Smoothing")]
    [SerializeField] private float groundedBufferTime = 0.12f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private RaycastHit slopeHit;

    // Ground detection
    private bool onGround;
    private bool wasGrounded;
    private float groundedGrace = 0f;

    // Block tracking
    private BlockBehaviour currentBlock;
    private BlockBehaviour lastGroundedBlock;

    // New tracking sets
    private int lastActivatedBlockId = -1;         // which block we first landed on
    private bool leftGroundAfterActivate = false;  // did we go airborne since activation?

    private readonly HashSet<int> landedBlockIdsThisLanding = new();
    [SerializeField] private float externalDecay = 6f;   // 外力衰减(越大越快停)
    private Vector3 externalVelocity = Vector3.zero;     // 外力速度(击退/冲刺等)

    // 供外部调用：添加一次外力（世界方向）
    public void AddExternalImpulse(Vector3 worldDir, float speed)
    {
        externalVelocity += worldDir.normalized * speed;
    }

    // 如果你想固定持续时间而不是衰减，也可以用协程逐渐减到 0
    public void AddExternalImpulseForDuration(Vector3 worldDir, float speed, float duration)
    {
        StopCoroutine(nameof(ExternalForDuration));
        StartCoroutine(ExternalForDuration(worldDir.normalized * speed, duration));
    }
    private System.Collections.IEnumerator ExternalForDuration(Vector3 v, float duration)
    {
        externalVelocity += v;
        yield return new WaitForSeconds(duration);
        externalVelocity -= v;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
{
    float dt = Time.deltaTime;

    // --- Ground Check with Smoothing ---
    onGround = controller.isGrounded;

    // Ground grace period (prevents bobbing flickering)
    if (onGround) groundedGrace = groundedBufferTime;
    else          groundedGrace -= dt;

    bool groundedSmoothed = onGround || groundedGrace > 0f;

    // Air to Ground transition
    if (groundedSmoothed && !wasGrounded)
    {
        if (animator) animator.SetBool("isJumping", false);
        landedBlockIdsThisLanding.Clear();
    }

    // Gentle stick to ground
    if (groundedSmoothed && velocity.y < 0f)
        velocity.y = -2f;

    // --- User Input ---
    float moveX = Input.GetAxisRaw("Horizontal");
    float moveZ = Input.GetAxisRaw("Vertical");

    // --- Camera-relative movement ---
    if (cameraTransform == null && Camera.main != null)
        cameraTransform = Camera.main.transform;

    Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
    Vector3 camRight   = cameraTransform.right;   camRight.y   = 0f; camRight.Normalize();

    Vector3 moveDir = camForward * moveZ + camRight * moveX;
    if (moveDir.magnitude > 1f) moveDir.Normalize();

    // --- Sprint check ---
    bool isSprinting = Input.GetKey(KeyCode.LeftShift) && moveDir.magnitude > 0.1f;
    float currentSpeed = isSprinting ? speed * sprintMultiplier : speed;
    if (animator) animator.SetBool("isSprinting", isSprinting);

    // --- Rotate Towards Movement Direction ---
    if (moveDir.sqrMagnitude > 0.001f)
    {
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * dt);
    }

    // --- Detect Block Under Feet (BEFORE jump check) ---
    UpdateCurrentBlockUnderFeet(groundedSmoothed);

    // --- Jump Logic ---
    if (Input.GetButtonDown("Jump") && groundedSmoothed)
    {
        if (currentBlock != null && currentBlock.IsActivated && !currentBlock.ExplodeOnLanding)
            currentBlock.ApplySecondStepIfReady();

        velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        if (animator) animator.SetBool("isJumping", true);
    }

    // --- Gravity ---
    velocity.y += gravity * dt;

    // ========== 关键改动：把水平(输入) + 外力 + 垂直 合成一次 Move ==========
    Vector3 planarByInput = moveDir * currentSpeed;                 // 水平（输入）
    Vector3 externalPlanar = new Vector3(externalVelocity.x, 0f, externalVelocity.z);
    float   verticalTotal  = velocity.y + externalVelocity.y;       // 外力若含竖直成分则叠加

    Vector3 finalVelocity = planarByInput + externalPlanar;         // 合成水平
    finalVelocity.y = verticalTotal;

    controller.Move(finalVelocity * dt);                            // ✅ 只 Move 一次

    // --- Platform Carrying (ride bobbing cubes) ---
    if (currentBlock != null && groundedSmoothed)
    {
        Vector3 platformDelta = currentBlock.Delta;
        if (platformDelta != Vector3.zero)
            controller.Move(platformDelta);  // 这是位移量，不是速度，单独再Move一次
    }

    // --- Slope Sliding ---
    HandleSlopeSliding();

    // --- Animator Updates ---
    if (animator)
    {
        animator.SetBool("isRunningBackwards", moveZ < -0.1f);
        animator.SetFloat("Speed", new Vector3(moveDir.x, 0f, moveDir.z).magnitude);
    }

    // --- Ground to Air ---
    if (!groundedSmoothed && wasGrounded)
        currentBlock = null;

    wasGrounded = groundedSmoothed;

    // === 外力衰减（放在最后，逐步回零） ===
    if (externalVelocity.sqrMagnitude > 0.0001f)
    {
        float decay = Mathf.Clamp01(externalDecay * dt);
        externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, decay);
    }
    else externalVelocity = Vector3.zero;
}

    void UpdateCurrentBlockUnderFeet(bool isGrounded)
    {
        Vector3 origin = transform.position + Vector3.up * 0.3f;
        float radius = 0.3f;
        float distance = groundCheckDistance;

        BlockBehaviour detectedBlock = null;

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, distance))
        {
            detectedBlock =
                hit.collider.GetComponentInParent<BlockBehaviour>() ??
                hit.collider.GetComponent<BlockBehaviour>() ??
                hit.collider.GetComponentInChildren<BlockBehaviour>();
        }

        if (detectedBlock == null)
        {
            Vector3 feet = transform.position +
                        Vector3.down * (controller.height * 0.5f - controller.skinWidth - 0.05f);
            foreach (var c in Physics.OverlapSphere(feet, 0.35f))
            {
                var b = c.GetComponentInParent<BlockBehaviour>() ??
                        c.GetComponent<BlockBehaviour>() ??
                        c.GetComponentInChildren<BlockBehaviour>();
                if (b) { detectedBlock = b; break; }
            }
        }

        if (isGrounded)
            currentBlock = detectedBlock;

        var thisFrameBlock = isGrounded ? currentBlock : null;
        if (thisFrameBlock != lastGroundedBlock)
            lastGroundedBlock = thisFrameBlock;
    }

    void HandleSlopeSliding()
    {
        if (currentBlock != null) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out slopeHit, controller.height / 2f + 0.5f))
        {
            float angle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (angle > slopeLimit)
            {
                Vector3 slideDir = Vector3.Cross(
                    Vector3.Cross(slopeHit.normal, Vector3.down),
                    slopeHit.normal
                ).normalized;

                controller.Move(slideDir * slideSpeed * Time.deltaTime);
            }
        }
    }
}