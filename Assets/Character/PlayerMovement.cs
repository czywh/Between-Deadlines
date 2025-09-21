using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 2f;
    public float gravity = -20f;
    public float rotationSpeed = 240f;
    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool onGround;

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
            velocity.y = -2f;
        }

        // player movement input 
        float moveX = Input.GetAxis("Horizontal"); // A or D keys
        float moveZ = Input.GetAxis("Vertical"); // W or S keys 

        // rotate left or right
        if (Mathf.Abs(moveX) > 0.1f)
        {
            transform.Rotate(Vector3.up * moveX * rotationSpeed * Time.deltaTime);
        }

        Vector3 move = transform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime);

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

        float movementMagnitude = new Vector3(move.x, 0, move.z).magnitude;
        animator.SetFloat("Speed", movementMagnitude);
    }
}
