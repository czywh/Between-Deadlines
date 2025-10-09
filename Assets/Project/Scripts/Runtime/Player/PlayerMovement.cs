using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{

	[Header("Refs")]
    public Transform cameraTransform;

	[Header("Movement")]
	public float speed = 3f;
	public float rotationSpeed = 360f;

	[Header("Jump & Gravity")]
    public float jumpForce = 1f;
    public float gravity = -20f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool onGround;

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
        // check if player on ground
        onGround = controller.isGrounded;
        if (onGround && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // player movement input
        float moveX = Input.GetAxisRaw("Horizontal"); // A or D keys
        float moveZ = Input.GetAxisRaw("Vertical"); // W or S keys 

        // movement direction from both inputs
        Vector3 move = new Vector3(moveX, 0, moveZ).normalized;
        controller.Move(move * speed * Time.deltaTime);

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

        // running backwards movement
        if (moveZ < -0.1f)
        {
            animator.SetBool("isRunningBackwards", true);
        }
        else{
            animator.SetBool("isRunningBackwards", false);
        }

        // apply gravity 
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        float movementMag = new Vector3(move.x, 0, move.z).magnitude;
        animator.SetFloat("Speed", movementMag);
    }
}
