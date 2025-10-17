using UnityEngine;

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
		
		if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        // --- Ground Check ---
        onGround = controller.isGrounded;
        if (onGround && velocity.y < 0)
        {
            velocity.y = -5f;
        }

        // --- User Input --- 
        float moveX = Input.GetAxisRaw("Horizontal"); // A or D keys
        float moveZ = Input.GetAxisRaw("Vertical"); // W or S keys 

        // --- Camera-relative movement --- 
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 move = camForward * moveZ + camRight * moveX;
        if (move.magnitude > 1f) move.Normalize();

        // --- Sprint check ---
        float currentSpeed = speed;
        bool isSprinting = false;

        if (Input.GetKey(KeyCode.LeftShift) && move.magnitude > 0.1f)
        {
            isSprinting = true;
        }
        animator.SetBool("isSprinting", isSprinting);

        // --- Move --- 
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- Rotate Towards Movement Direction ---
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // --- Jumping movement --- 
        if (Input.GetButtonDown("Jump") && onGround)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            animator.SetBool("isJumping", true);
        }
        else if (onGround)
        {
            animator.SetBool("isJumping", false);
        }

        // --- Running backwards movement ---
        if (moveZ < -0.1f)
        {
            animator.SetBool("isRunningBackwards", true);
        }
        else{
            animator.SetBool("isRunningBackwards", false);
        }

        // --- Apply gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        HandleSlopeSliding();

        float movementMag = new Vector3(move.x, 0, move.z).magnitude;
        animator.SetFloat("Speed", movementMag);
    }

    void HandleSlopeSliding()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out slopeHit, controller.height / 2f + 0.5f))
        {
            float angle = Vector3.Angle(slopeHit.normal, Vector3.up);

            if (angle > slopeLimit)
            {
                // Get downhill direction along the slope
                Vector3 slideDir = Vector3.Cross(Vector3.Cross(slopeHit.normal, Vector3.down), slopeHit.normal).normalized;
                controller.Move(slideDir * slideSpeed * Time.deltaTime);
            }
        }
    }

}