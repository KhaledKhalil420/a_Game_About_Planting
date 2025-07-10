using System.Collections;
using DG.Tweening;
using UnityEngine;

public class PlayerMovement : PlayerMovementVariables
{
    private void Awake()
    {
        playerMovement = this;
    }

    private void LateUpdate()
    {
        Inputs();
    }

    private void FixedUpdate()
    {
        ApplyCounterMovement();
        MovePlayer();
        HandleFootsteps();
    }

    #region Movement Mechanics

    private void Inputs()
    {
        HandleRunning();
        HandleDirection();
        HandleJumping();
        HandleCrouching();
        HandleSliding();
    }

    private void HandleRunning()
    {
        isRunning = Input.GetButton("Running");
    }

    private void HandleDirection()
    {
        inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), inputDir.y, Input.GetAxisRaw("Vertical"));
        moveDir = inputDir.x * transform.right + inputDir.z * transform.forward;
    }

    private void HandleJumping()
    {
        if (!enableJumping) return;

        if (Input.GetButton("Jump") && rb.linearVelocity.y <= 0.1f & IsGrounded())
        {
            isCrouching = false;
            playerCollider.height = 2f;
            playerCollider.center = Vector3.zero;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce * Time.fixedDeltaTime, rb.linearVelocity.z);
        }
    }

    private void HandleCrouching()
    {
        if (!enableCrouching) return;

        if (Input.GetButtonDown("Crouch") && !isSliding && IsGrounded())
        {
            isCrouching = !isCrouching;

            if (isCrouching)
            {
                playerCollider.height = 1f;
                playerCollider.center = new Vector3(0, -0.5f, 0);
                playerLook._mainCamera.transform.DOLocalMove(new Vector3(0, -0.15f, 0), 0.1f).OnComplete(() =>
                {
                    playerLook._mainCamera.transform.DOLocalMove(Vector3.zero, 0.15f);
                });
            }

            if (!isCrouching)
            {
                if (IsHeaded())
                {
                    isCrouching = true;
                    StartCoroutine(TryToStopCrouch());
                }
                else
                {
                    playerCollider.height = 2f;
                    playerCollider.center = Vector3.zero;
                    isCrouching = false;
                    playerLook._mainCamera.transform.DOLocalMove(new Vector3(0, -0.15f, 0), 0.1f).OnComplete(() =>
                    {
                        playerLook._mainCamera.transform.DOLocalMove(Vector3.zero, 0.15f);
                    });
                }
            }
        }
    }

    private Tween slideTween;

    private void HandleSliding()
    {
        if (!enableSliding) return;

        if (Input.GetButton("Crouch") && isRunning && IsGrounded())
        {
            if (isSliding) return;

            isSliding = true;
            playerCollider.height = 1f;
            playerCollider.center = new Vector3(0, -0.5f, 0);
            playerLook._mainCamera.transform.DOLocalRotate(new Vector3(0, 0, 4), 0.25f).SetEase(Ease.OutSine);

            slideTween = DOTween.To(() => currentSlideForce, x => currentSlideForce = x, 0f, slideTime).SetAutoKill(true)
                .OnUpdate(() =>
                {
                    if (IsGrounded())
                        rb.AddForce(currentSlideForce * Time.deltaTime * moveDir.normalized, ForceMode.Impulse);
                })
                .OnComplete(() =>
                {
                    isSliding = false;
                    StartCoroutine(TryToStopCrouch());
                })
                .OnKill(() =>
                {
                    isSliding = false;
                    StartCoroutine(TryToStopCrouch());
                    playerLook._mainCamera.transform.DOLocalRotate(Vector3.zero, 0.25f).SetEase(Ease.InSine);
                });
        }

        if (Input.GetButtonUp("Crouch") && isSliding)
        {
            slideTween.Kill();
        }

        if (!isSliding)
        {
            currentSlideForce = slideForce;
        }
    }

    private IEnumerator TryToStopCrouch()
    {
        if (!IsHeaded())
            yield return null;

        while (isCrouching)
        {
            if (!IsHeaded())
            {
                playerCollider.height = 2f;
                playerCollider.center = Vector3.zero;
                isCrouching = false;
                playerLook._mainCamera.transform.DOLocalMove(new Vector3(0, -0.15f, 0), 0.1f).OnComplete(() =>
                {
                    playerLook._mainCamera.transform.DOLocalMove(Vector3.zero, 0.15f);
                });
            }

            yield return new WaitForEndOfFrame();
        }
    }

    #endregion

    #region Footsteps

    private void HandleFootsteps()
    {
        if (!IsGrounded() || moveDir.magnitude < 0.1f || isSliding) return;

        footstepTimer -= Time.fixedDeltaTime;

        if (footstepTimer <= 0f)
        {
            footstepTimer = FootstepsCooldown();

            if (footstepsSounds.Length > 0)
            {
                footstepsSource.pitch = Random.Range(0.95f, 1.05f);
                footstepsSource.PlayOneShot(footstepsSounds[Random.Range(0, footstepsSounds.Length)]);
            }
        }
    }

    #endregion
}