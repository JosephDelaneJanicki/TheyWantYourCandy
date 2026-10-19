
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 7f;
    public float gravity = -25f;
    public float rotationSpeed = 12f;
    public Transform cameraTransform;

    [Header("Animation")]
    public Animator animator;
    public float animationSmoothTime = 0.15f;

    private CharacterController controller;
    private float verticalVelocity;

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private static readonly int GroundedHash =
        Animator.StringToHash("Grounded");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Find the Animator on the character model if not assigned.
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        else
        {
            Debug.LogWarning(
                "PlayerMovement: No Animator found on the player.",
                this
            );
        }
    }

    private void Update()
    {
        // Read movement input.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Determine camera-relative movement directions.
        Vector3 forward = cameraTransform
            ? cameraTransform.forward
            : Vector3.forward;

        Vector3 right = cameraTransform
            ? cameraTransform.right
            : Vector3.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = Vector3.ClampMagnitude(
            right * h + forward * v,
            1f
        );

        // Rotate the player toward the movement direction.
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(move);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Handle gravity.
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        // Apply movement through the CharacterController.
        Vector3 velocity =
            move * speed + Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // Update character animations.
        UpdateAnimations(move);
    }

    private void UpdateAnimations(Vector3 move)
    {
        if (animator == null)
            return;

        // 0 = Idle
        // 0.333 = Walk
        // 1 = Run
        float movementAmount = move.magnitude;

        animator.SetFloat(
            MoveSpeedHash,
            movementAmount,
            animationSmoothTime,
            Time.deltaTime
        );

        animator.SetBool(
            GroundedHash,
            controller.isGrounded
        );
    }
}
