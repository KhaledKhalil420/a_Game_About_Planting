using UnityEngine;

[System.Serializable]
public class PlayerMovementVariables : MonoBehaviour
{
    public static PlayerMovement playerMovement;


    public enum PlayerMovementState
    {
        Idle,
        Walking,
        Running,
        OnAir,
        Crouching,
        Sliding
    }

    private void Update()
    {
        HandleStates();
        HandleSpeed();
    }

    #region State Variables

    [Header("Movement Modules")]
    [SerializeField] protected bool enableJumping = true;
    [SerializeField] protected bool enableCrouching = true;
    [SerializeField] protected bool enableSliding = true;

    [Header("State")]
    [SerializeField] private PlayerMovementState state;

    internal Vector3 inputDir;
    internal Vector3 moveDir;
    internal float currentSpeed;
    protected float currentSlideForce;

    internal bool isCrouching = false;
    internal bool isRunning = false;
    internal bool isSliding = false;

    #endregion

    #region Movement Speeds

    [Header("Forces")]
    [SerializeField] protected float walkingSpeed;
    [SerializeField] protected float runSpeed;
    [SerializeField] protected float airSpeed;
    [SerializeField] protected float crouchSpeed;
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float slideForce;
    [SerializeField] protected float slideTime;

    #endregion

    #region Game Feel

    [Header("Game Feel")]
    [SerializeField] protected float bufferTime;
    [SerializeField] protected float cayoteTime;
    protected float bufferTimer = 0;
    protected float cayoteTimer = 0;

    [Header("Counter Movement")]
    [SerializeField] protected float groundDrag = 6f;
    [SerializeField] protected float counterMovementForce = 10f;

    #endregion

    #region References

    [Header("References")]
    [SerializeField] protected Transform feet;
    [SerializeField] protected Transform head;
    public Rigidbody rb;
    [SerializeField] protected LayerMask unJumpableLayers;
    public CapsuleCollider playerCollider;
    public PlayerLook playerLook;

    #endregion

    #region Sounds

    [Header("Sounds")]
    [SerializeField] protected AudioClip[] footstepsSounds;
    [SerializeField] protected AudioSource footstepsSource;
    [SerializeField] protected float footstepsCoolDownWalking = 0.4f;
    [SerializeField] protected float footstepsCoolDownRunning = 0.15f;
    protected float footstepTimer;

    #endregion

    #region Moving

    protected void ApplyCounterMovement()
    {
        if (IsGrounded())
        {
            rb.linearDamping = groundDrag;

            if (moveDir.magnitude < 0.1f)
            {
                Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                if (horizontalVel.magnitude > 0.1f)
                {
                    Vector3 counterForce = -horizontalVel.normalized * counterMovementForce;
                    rb.AddForce(counterForce, ForceMode.Acceleration);
                }
            }
        }
        else
        {
            rb.linearDamping = 0f;
        }
    }

    protected void MovePlayer()
    {
        rb.AddForce(moveDir.normalized * currentSpeed, ForceMode.Acceleration);
    }

    #endregion

    #region States

    private void SetState(PlayerMovementState newState) => state = newState;

    private void HandleStates()
    {
        if (!IsGrounded())
        {
            SetState(PlayerMovementState.OnAir);
            return;
        }

        if (enableSliding && IsSliding()) return;
        if (enableCrouching && IsCrouching()) return;
        if (IsIdle()) return;
        if (IsRunning()) return;

        SetState(PlayerMovementState.Walking);
    }

    private bool IsCrouching()
    {
        if (isCrouching)
        {
            SetState(PlayerMovementState.Crouching);
            return true;
        }
        return false;
    }

    private bool IsIdle()
    {
        if (moveDir.magnitude == 0)
        {
            SetState(PlayerMovementState.Idle);
            return true;
        }
        return false;
    }

    private bool IsRunning()
    {
        if (isRunning)
        {
            SetState(PlayerMovementState.Running);
            return true;
        }
        return false;
    }

    private bool IsSliding()
    {
        if (isSliding)
        {
            SetState(PlayerMovementState.Sliding);
            return true;
        }
        return false;
    }

    #endregion

    #region Speed

    private void HandleSpeed()
    {
        switch (state)
        {
            case PlayerMovementState.Idle:
                currentSpeed = 0;
                break;

            case PlayerMovementState.Walking:
                currentSpeed = walkingSpeed;
                break;

            case PlayerMovementState.Running:
                currentSpeed = runSpeed;
                break;

            case PlayerMovementState.OnAir:
                currentSpeed = airSpeed;
                break;

            case PlayerMovementState.Crouching:
                currentSpeed = crouchSpeed;
                break;

            case PlayerMovementState.Sliding:
                currentSpeed = airSpeed;
                break;
        }
    }

    #endregion

    #region Collision Detection

    public bool IsGrounded()
    {
        return Physics.CheckSphere(feet.position, 0.45f, ~unJumpableLayers);
    }

    public bool IsHeaded()
    {
        return Physics.CheckSphere(playerCollider.bounds.center + new Vector3(0, playerCollider.bounds.extents.y, 0), 0.45f, ~unJumpableLayers);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsHeaded() ? Color.red : Color.green;
        Gizmos.DrawWireSphere(playerCollider.bounds.center + new Vector3(0, playerCollider.bounds.extents.y, 0), 0.45f);
    }

    #endregion

    #region Footsteps

    public float FootstepsCooldown()
    {
        return isRunning ? footstepsCoolDownRunning : footstepsCoolDownWalking;
    }
     
    #endregion
}