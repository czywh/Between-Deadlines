using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{

    [Header("Movement Settings")]
    public float speed = 3f;
    public float jumpForce = 1f;
    public float gravity = -20f;
    public float rotationSpeed = 360f;
    public float sprintMultiplier = 2f; 

    [Header("Slope & Sliding Settings")]
    public float slopeLimit = 8f;
    public float slideSpeed = 4f;
    public float groundCheckDistance = 1.2f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool onGround;
    private RaycastHit slopeHit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // check if player on ground
        onGround = controller.isGrounded;
        if (onGround && velocity.y < 0)
        {
            velocity.y = -5f;
        }

        // player movement input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // camera-relative movement direction
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        Vector3 move = (camForward * moveZ + camRight * moveX).normalized;

        //sprint check
        float currentSpeed = speed;
        bool isSprinting = false;

        // if "shift" key is held, increase speed
        if (Input.GetKey(KeyCode.LeftShift) && move.magnitude > 0.1f)
        {
            currentSpeed *= sprintMultiplier;
            isSprinting = true;
        }
        animator.SetBool("isSprinting", isSprinting);
        controller.Move(move * currentSpeed * Time.deltaTime);

        // rotate to face direction of movement
        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // jumping movement
        if (Input.GetButtonDown("Jump") && onGround)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            animator.SetBool("isJumping", true);
        }
        else if (onGround)
        {
            animator.SetBool("isJumping", false);
        }

        // apply gravity 
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // slope sliding
        HandleSlopeSliding();

        float movementMag = new Vector3(move.x, 0, move.z).magnitude;
        animator.SetFloat("Speed", movementMag * (isSprinting ? sprintMultiplier : 1f));
    }

    void HandleSlopeSliding()
    {
        // Start ray from just above feet
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out slopeHit, controller.height / 2f + 0.5f))
        {
            float angle = Vector3.Angle(slopeHit.normal, Vector3.up);

            // Trigger sliding when slope exceeds threshold
            if (angle > slopeLimit)
            {
                // Get downhill direction along the surface
                Vector3 slideDir = Vector3.Cross(Vector3.Cross(slopeHit.normal, Vector3.down), slopeHit.normal).normalized;
                controller.Move(slideDir * slideSpeed * Time.deltaTime);
            }
        }
    }
}
