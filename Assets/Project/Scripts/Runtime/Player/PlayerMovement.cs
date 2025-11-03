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

    private readonly HashSet<int> landedBlockIdsThisLanding = new();

    [SerializeField] private KnockbackReceiver knockback;

    void Awake()
    {
        if (!knockback) knockback = GetComponent<KnockbackReceiver>();
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
        // --- Ground Check with Smoothing ---
        onGround = controller.isGrounded;

        // Ground grace period (prevents bobbing flickering)
        if (onGround)
            groundedGrace = groundedBufferTime;
        else
            groundedGrace -= Time.deltaTime;

        bool groundedSmoothed = onGround || groundedGrace > 0f;

        // Air to Ground transition
        if (groundedSmoothed && !wasGrounded)
        {
            if (animator) animator.SetBool("isJumping", false);
            landedBlockIdsThisLanding.Clear();
        }


        // Gentle stick to ground
        if (groundedSmoothed && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // --- User Input ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // --- Camera-relative movement ---
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 move = camForward * moveZ + camRight * moveX;
        if (move.magnitude > 1f) move.Normalize();

        // --- Sprint check ---
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && move.magnitude > 0.1f;
        float currentSpeed = isSprinting ? speed * sprintMultiplier : speed;
        animator.SetBool("isSprinting", isSprinting);

        // --- Move ---
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- Platform Carrying (ride bobbing cubes) ---
        if (currentBlock != null && groundedSmoothed)
        {
            Vector3 platformDelta = currentBlock.Delta;
            if (platformDelta != Vector3.zero)
            {
                controller.Move(platformDelta);
            }
        }

        // --- Rotate Towards Movement Direction ---
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // --- Detect Block Under Feet (BEFORE jump check) ---
        UpdateCurrentBlockUnderFeet(groundedSmoothed);

        // --- Jump Logic (with cube explosion on 2nd press) ---
        if (Input.GetButtonDown("Jump") && groundedSmoothed)
        {
            if (currentBlock != null && currentBlock.IsActivated && !currentBlock.ExplodeOnLanding)
            {
                currentBlock.ApplySecondStepIfReady();
            }

            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            if (animator) animator.SetBool("isJumping", true);
        }

        // --- Apply gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (knockback != null)
        {
            Vector3 knockDelta = knockback.ConsumeDelta(Time.deltaTime);
            if (knockDelta.sqrMagnitude > 0f)
                controller.Move(knockDelta);
        }

        // --- Slope Sliding ---
        HandleSlopeSliding();

        // --- Animator Updates ---
        animator.SetBool("isRunningBackwards", moveZ < -0.1f);
        animator.SetFloat("Speed", new Vector3(move.x, 0, move.z).magnitude);

        // --- Ground to Air ---
        if (!groundedSmoothed && wasGrounded)
        {
            currentBlock = null;
        }

        wasGrounded = groundedSmoothed;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Downward/top contact checks (keep yours)
        if (hit.moveDirection.y > -0.05f && velocity.y > -0.01f) return;
        float upDot = Vector3.Dot(hit.normal, Vector3.up);
        if (upDot < 0.8f) return;

        var block =
            hit.collider.GetComponentInParent<BlockBehaviour>() ??
            hit.collider.GetComponent<BlockBehaviour>() ??
            hit.collider.GetComponentInChildren<BlockBehaviour>();
        if (block == null) return;

        // once-per-landing guard (see Fix #2 below for the new set)
        int key = block.transform.root.gameObject.GetInstanceID();
        if (!landedBlockIdsThisLanding.Add(key)) return;

        if (block.ExplodeOnLanding)
        {
            block.BreakNow();
        }
        else if (!block.IsActivated)
        {
            block.ApplyFirstStepIfNeeded();
        }
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