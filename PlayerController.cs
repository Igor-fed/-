using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float gravity = -20f;


    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Animation Speed")]
    [SerializeField] private float walkAnimSpeed = 1.0f;
    [SerializeField] private float runAnimSpeed = 1.8f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.4f; 
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundMask;


    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.3f;     // Задержка между атаками
    [SerializeField] private float attackAnimationLength = 0.5f; // Длина анимации атаки

    // Components
    private CharacterController controller;
    private Animator animator;
    private Transform cameraTransform;

    // Input
    private Vector2 moveInput;
    private bool sprintHeld;
    private bool crouchHeld;   // удержание — НЕ сбрасывается каждый кадр
    private bool jumpPressed;
    private bool lmbPressed;
    private bool lmbHeld;      // удержание ЛКМ для цикличной анимации
    private bool rmbPressed;

    // State
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
  
    private bool isSprinting;
    private float targetHeight;

    // Animation hashes
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsCrouching = Animator.StringToHash("IsCrouching");
    private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int HashInteract = Animator.StringToHash("Interact");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashIsBreaking = Animator.StringToHash("IsBreaking");

    private CharacterController capsule;
    private float lastAttackTime;
    private bool isAttacking;
    private Coroutine attackCoroutine;
    private float lastAltAttackTime;
    private Coroutine resetAnimationCoroutine;

    // ───────────────────────────────────────────────
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        cameraTransform = Camera.main.transform;
        capsule = GetComponent<CharacterController>();


        targetHeight = standingHeight;
    }

    // ── Input callbacks (PlayerInput → Send Messages) ──
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnSprint(InputValue value) => sprintHeld = value.isPressed;
    public void OnJump(InputValue value) { if (value.isPressed) jumpPressed = true; }

    // Crouch — удержание (не toggle)
    public void OnCrouch(InputValue value) => crouchHeld = value.isPressed;

    // Attack — одиночное нажатие + удержание
    // Добавьте ЭТОТ метод в ваш скрипт (рядом с другими On... методами):

    // AttackHold - для зажатия ЛКМ (циклическая анимация)
    public void OnAttackHold(InputValue value)
    {
        if (value.isPressed)
        {
            lmbHeld = true;
            if (!isAttacking)
            {
                StartAttack();
            }
        }
        else
        {
            lmbHeld = false;
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
                isAttacking = false;
            }
            // Не выключаем IsBreaking, потому что его нет
        }
    }

    // Attack - для одиночного нажатия (если нужно)
    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("Attack Pressed (single click)");
            PerformAttack(); // Одиночная атака
        }
    }

    // AttackAlt - для ПКМ (оставляем как есть)
    public void OnAttackAlt(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("AttackAlt Pressed");
            PerformAltAttack();
        }
    }
    // ────────────────────────────────────────────────
    private void Update()
    {
        if (Time.frameCount % 60 == 0) // раз в секунду
        {
            Debug.Log($"lmbHeld = {lmbHeld}, isAttacking = {isAttacking}");
        }
        CheckGround();
        HandleJump();
        HandleCrouch();
        HandleMovement();
   
        ApplyGravity();
        UpdateAnimations();

        // Сброс одноразовых нажатий (НЕ трогаем crouchHeld и lmbHeld!)
        jumpPressed = false;
        lmbPressed = false;
        rmbPressed = false;


    }

    // ── Ground ──────────────────────────────────────
    private void CheckGround()
    {
        Vector3 capsuleBottom = transform.position + capsule.center - Vector3.up * (capsule.height / 2f - 0.05f);
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(capsuleBottom, groundCheckRadius, groundMask);

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }
    // ── Jump ────────────────────────────────────────
    private void HandleJump()
    {
        if (jumpPressed && isGrounded && !isCrouching && velocity.y <= 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressed = false; // Сразу сбрасываем, чтобы не было двойного прыжка
        }
    }

    // ── Movement ────────────────────────────────────
    private void HandleMovement()
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        isSprinting = sprintHeld && !isCrouching && moveInput.magnitude > 0.1f;

        float speed = isCrouching ? crouchSpeed : (isSprinting ? runSpeed : walkSpeed);
        controller.Move(moveDir * speed * Time.deltaTime);

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }

        float normalizedSpeed = moveInput.magnitude;
        float targetAnimSpeed = normalizedSpeed > 0.1f
            ? (isSprinting ? runAnimSpeed : walkAnimSpeed)
            : 0f;

        if (Mathf.Abs(targetAnimSpeed) < 0.001f)
            animator.SetFloat(HashSpeed, 0f);
        else
            animator.SetFloat(HashSpeed, targetAnimSpeed, 0.1f, Time.deltaTime);

        animator.SetBool(HashIsMoving, normalizedSpeed > 0.1f);
    }

    // ── Gravity ─────────────────────────────────────
    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // ── Crouch ──────────────────────────────────────
    private void HandleCrouch()
    {
        if (crouchHeld && !isCrouching)
        {
            isCrouching = true;
            targetHeight = crouchHeight;
            capsule.height = crouchHeight;
        }
        else if (!crouchHeld && isCrouching)
        {
            isCrouching = false;
            targetHeight = standingHeight;
            capsule.height = standingHeight;
        }

        if (isSprinting && isCrouching)
        {
            isCrouching = false;
            targetHeight = standingHeight;
            capsule.height = standingHeight;
        }

        
    }
    private bool CanStandUp()
    {
        float headRoom = standingHeight - controller.height;
        return !Physics.Raycast(transform.position, Vector3.up, headRoom + 0.1f);
    }
    //── Attack ─────────────────────────────────
    private void StartAttack()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;
        while (lmbHeld)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;

                // Только триггер
                animator.ResetTrigger(HashInteract);
                animator.SetTrigger(HashInteract);

                yield return new WaitForSeconds(attackAnimationLength);
            }
            else
            {
                float waitTime = attackCooldown - (Time.time - lastAttackTime);
                yield return new WaitForSeconds(waitTime);
            }
        }
        isAttacking = false;
    }



    private void PerformAttack()
    {
        Debug.Log("Attack performed!");

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
            }
        }
    }

    private void PerformAltAttack()
    {
        if (Time.time - lastAltAttackTime >= attackCooldown)
        {
            lastAltAttackTime = Time.time;
            Debug.Log("Alt Attack performed!");

            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                    interactable.InteractAlt(this);
            }

            // Только триггер
            animator.ResetTrigger(HashInteract);
            animator.SetTrigger(HashInteract);
        }
    }





    // ── Animations ──────────────────────────────────
    private void UpdateAnimations()
    {
        animator.SetBool(HashIsCrouching, isCrouching);
        animator.SetBool(HashIsGrounded, isGrounded);
    }

    // ── Gizmos ──────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        // Если игра не запущена, используем standingHeight
        if (!Application.isPlaying && capsule == null)
        {
            Vector3 capsuleBottom = transform.position + Vector3.down * (standingHeight / 2f);
            Gizmos.DrawWireSphere(capsuleBottom, groundCheckRadius);
        }
        else if (capsule != null)
        {
            Vector3 capsuleBottom = transform.position + capsule.center - Vector3.up * (capsule.height / 2f);
            Gizmos.DrawWireSphere(capsuleBottom, groundCheckRadius);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

// ── Interface ───────────────────────────────────────
public interface IInteractable
{
    void Interact(PlayerController player);
    void InteractAlt(PlayerController player);
}