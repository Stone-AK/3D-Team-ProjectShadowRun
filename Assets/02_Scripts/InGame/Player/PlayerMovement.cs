using UnityEngine;

public enum PlayerPosture
{
    Standing,
    Crouching,
    Prone
}

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler InputHandler;
    [SerializeField] private PlayerAnimeController AnimeController;

    [Header("Movement")]
    [SerializeField] private float WalkSpeed = 2f;
    [SerializeField] private float MoveSpeed = 5f;
    [SerializeField] private float SprintSpeed = 8f;
    [SerializeField] private float JumpPower = 5f;

    private float _speedBoost;
    private float _speedBoostEndTime;

    public bool IsSpeedBoostActive => _speedBoost > 0f;
    public event System.Action<bool> SpeedBoostStateChanged;

    [Header("Stamina")]
    [SerializeField] private float SprintStaminaUsePerSecond = 20f;
    [SerializeField] private float StaminaRecoveryPerSecond = 15f;
    [SerializeField] private float SprintRestartStaminaRatio = 0.2f;
    private PlayerStatus _playerStatus;
    private bool _isSprintExhausted;

    [Header("Ground Check")]
    [SerializeField] private Transform GroundCheck;
    [SerializeField] private float GroundRadius = 0.2f;
    [SerializeField] private LayerMask GroundMask;
    [SerializeField] private float GroundingForce = 50f;
    [SerializeField] private float GroundingDisableDuration = 0.15f;
    [SerializeField] private float GroundingReleaseDelay = 0.1f;

    private bool _isGrounded;
    private float _groundingDisabledUntil;
    private float _lastMoveInputTime;
    private Rigidbody _rigidbody;

    [Header("Fall Damage")]
    [SerializeField] private float SafeFallSpeed = 8f;
    [SerializeField] private float FallDamagePerSpeed = 10f;

    private bool _wasGrounded;
    private float _maximumFallSpeed;

    [Header("Posture")]
    [SerializeField] private CapsuleCollider StandingCollider;
    [SerializeField] private CapsuleCollider ProneCollider;
    [SerializeField] private Transform Head;
    [SerializeField, Range(0.1f, 1f)]
    private float CrouchHeightRatio = 0.5f;
    [SerializeField, Range(0.1f, 1f)]
    private float CrouchHeadHeightRatio = 0.7f;
    [SerializeField, Range(0.1f, 1f)]
    private float ProneHeadHeightRatio = 0.25f;
    [SerializeField] private float PostureChangeSpeed = 8f;
    [SerializeField] private float CrouchSpeed = 3f;
    [SerializeField] private float ProneSpeed = 1.5f;
    private PlayerPosture _currentPosture = PlayerPosture.Standing;
    private float _standingColliderHeight;
    private Vector3 _standingColliderCenter;
    private float _crouchColliderHeight;
    private Vector3 _crouchColliderCenter;
    private Vector3 _standingHeadPosition;
    private Vector3 _crouchHeadPosition;
    private Vector3 _proneHeadPosition;
    private float _targetColliderHeight;
    private Vector3 _targetColliderCenter;
    private Vector3 _targetHeadPosition;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _standingColliderHeight = StandingCollider.height;
        _standingColliderCenter = StandingCollider.center;
        _standingHeadPosition = Head.localPosition;

        _crouchColliderHeight = Mathf.Max(
            _standingColliderHeight * CrouchHeightRatio,
            StandingCollider.radius * 2f
        );

        _crouchColliderCenter = CalculateColliderCenter(_crouchColliderHeight);

        _crouchHeadPosition = _standingHeadPosition;
        _crouchHeadPosition.y = _standingHeadPosition.y * CrouchHeadHeightRatio;

        _proneHeadPosition = _standingHeadPosition;
        _proneHeadPosition.y = _standingHeadPosition.y * ProneHeadHeightRatio;

        _targetColliderHeight = _standingColliderHeight;
        _targetColliderCenter = _standingColliderCenter;
        _targetHeadPosition = _standingHeadPosition;

        _rigidbody = GetComponent<Rigidbody>();
        _playerStatus = GetComponent<PlayerStatus>();

        StandingCollider.enabled = true;
        ProneCollider.enabled = false;
    }

    private void OnEnable()
    {
        InputHandler.JumpPerformed += Jump;
        InputHandler.CrouchPerformed += ToggleCrouch;
        InputHandler.PronePerformed += ToggleProne;
    }

    private void OnDisable()
    {
        InputHandler.JumpPerformed -= Jump;
        InputHandler.CrouchPerformed -= ToggleCrouch;
        InputHandler.PronePerformed -= ToggleProne;

        if (_rigidbody != null)
            _rigidbody.useGravity = true;
    }
    private void Update()
    {
        UpdateHeadPosition();
        UpdateSpeedBoost();
    }


    private void FixedUpdate()
    {
        Move();
        UpdatePostureCollider();
        CheckGround();
        UpdateFallDamage();
        ApplyGroundingForce();
        UpdateIdleGroundHold();
    }

    private void CheckGround()
    {
        _isGrounded = Physics.CheckSphere(
            GroundCheck.position,
            GroundRadius,
            GroundMask
        );
    }

    private void ApplyGroundingForce()
    {
        bool hasMoveInput = InputHandler.MoveInput.sqrMagnitude > 0.01f;

        if (hasMoveInput)
            _lastMoveInputTime = Time.time;

        bool recentlyMoved = Time.time - _lastMoveInputTime <= GroundingReleaseDelay;

        if (!_isGrounded || !recentlyMoved || Time.time < _groundingDisabledUntil)
            return;

        _rigidbody.AddForce(
            Vector3.down * GroundingForce,
            ForceMode.Acceleration
        );
    }

    private void UpdateIdleGroundHold()
    {
        bool hasMoveInput = InputHandler.MoveInput.sqrMagnitude > 0.01f;
        bool isJumping = Time.time < _groundingDisabledUntil;
        bool shouldHoldPosition = _isGrounded && !hasMoveInput && !isJumping;

        _rigidbody.useGravity = !shouldHoldPosition;

        if (shouldHoldPosition)
            _rigidbody.linearVelocity = Vector3.zero;
    }

    private void UpdateFallDamage()
    {
        if (!_isGrounded)
        {
            float currentFallSpeed = -_rigidbody.linearVelocity.y;

            if (currentFallSpeed > _maximumFallSpeed)
                _maximumFallSpeed = currentFallSpeed;
        }
        else if (!_wasGrounded)
        {
            float dangerousFallSpeed = _maximumFallSpeed - SafeFallSpeed;

            if (dangerousFallSpeed > 0f)
            {
                float fallDamage = dangerousFallSpeed * FallDamagePerSpeed;
                _playerStatus.TakeDamage(fallDamage);
            }

            _maximumFallSpeed = 0f;
        }

        _wasGrounded = _isGrounded;
    }

    private void Move()
    {
        Vector2 input = InputHandler.MoveInput;

        Vector3 moveDir = transform.forward * input.y + transform.right * input.x;

        moveDir.Normalize();

        UpdateSprintExhaustion();

        bool wantsToSprint = _currentPosture == PlayerPosture.Standing && InputHandler.IsSprintPressed && input.y > 0f;

        bool isSprinting = wantsToSprint && !_isSprintExhausted && _playerStatus.Model.CurrentStamina > 0f;

        UpdateStamina(isSprinting);

        float currentSpeed = GetCurrentMoveSpeed(isSprinting);

        _rigidbody.linearVelocity = new Vector3(moveDir.x * currentSpeed, _rigidbody.linearVelocity.y, moveDir.z * currentSpeed
        );
    }

    private float GetCurrentMoveSpeed(bool isSprinting)
    {
        AnimeController.SetRun(isSprinting);

        float currentSpeed;

        if (_currentPosture == PlayerPosture.Prone)
            currentSpeed = ProneSpeed;
        else if (_currentPosture == PlayerPosture.Crouching)
            currentSpeed = CrouchSpeed;
        else if (isSprinting)
            currentSpeed = SprintSpeed;
        else if (InputHandler.IsWalkPressed)
            currentSpeed = WalkSpeed;
        else
            currentSpeed = MoveSpeed;

        // PlayerStatus에서 상태 이상으로 인한 속도 배율 가져오기
        float statusMultiplier = ( _playerStatus != null ) ? _playerStatus.SpeedMultiplier : 1f;

        // (기본 속도 + 아이템 부스트)에 상태 이상 배율을 곱해서 반환
        return ( currentSpeed + _speedBoost ) * statusMultiplier;
    }

    public void ApplySpeedBoost(float amount, float duration)
    {
        if (amount <= 0f || duration <= 0f)
            return;

        _speedBoost = amount;
        _speedBoostEndTime = Time.time + duration;
        SpeedBoostStateChanged?.Invoke(true);
    }

    private void UpdateSpeedBoost()
    {
        if (!IsSpeedBoostActive || Time.time < _speedBoostEndTime)
            return;

        _speedBoost = 0f;
        _speedBoostEndTime = 0f;
        SpeedBoostStateChanged?.Invoke(false);
    }

    private void Jump()
    {
        if (!_isGrounded)
            return;

        if (_currentPosture != PlayerPosture.Standing)
            return;

        _groundingDisabledUntil = Time.time + GroundingDisableDuration;
        _rigidbody.useGravity = true;
        _rigidbody.AddForce(Vector3.up * JumpPower, ForceMode.Impulse);
    }

    private Vector3 CalculateColliderCenter(float targetHeight)
    {
        float colliderBottom =
            _standingColliderCenter.y -
            (_standingColliderHeight * 0.5f);

        Vector3 targetCenter = _standingColliderCenter;

        targetCenter.y =
            colliderBottom +
            (targetHeight * 0.5f);

        return targetCenter;
    }

    private void ToggleCrouch()
    {
        if (_currentPosture == PlayerPosture.Crouching)
        {
            ChangePosture(PlayerPosture.Standing);
            return;
        }

        ChangePosture(PlayerPosture.Crouching);
    }

    private void ToggleProne()
    {
        if (_currentPosture == PlayerPosture.Prone)
        {
            ChangePosture(PlayerPosture.Standing);
            return;
        }

        ChangePosture(PlayerPosture.Prone);
    }

    private void ChangePosture(PlayerPosture newPosture)
    {
        _currentPosture = newPosture;

        switch (_currentPosture)
        {
            case PlayerPosture.Standing:
                ProneCollider.enabled = false;
                StandingCollider.enabled = true;

                _targetColliderHeight = _standingColliderHeight;
                _targetColliderCenter = _standingColliderCenter;
                _targetHeadPosition = _standingHeadPosition;
                break;

            case PlayerPosture.Crouching:
                ProneCollider.enabled = false;
                StandingCollider.enabled = true;

                _targetColliderHeight = _crouchColliderHeight;
                _targetColliderCenter = _crouchColliderCenter;
                _targetHeadPosition = _crouchHeadPosition;
                break;

            case PlayerPosture.Prone:
                StandingCollider.enabled = false;
                ProneCollider.enabled = true;

                _targetHeadPosition = _proneHeadPosition;
                break;
        }
    }

    private void UpdateHeadPosition()
    {
        Head.localPosition = Vector3.Lerp(
            Head.localPosition,
            _targetHeadPosition,
            PostureChangeSpeed * Time.deltaTime
        );
    }

    private void UpdatePostureCollider()
    {
        if (_currentPosture == PlayerPosture.Prone)
            return;

        StandingCollider.height = Mathf.Lerp(
            StandingCollider.height,
            _targetColliderHeight,
            PostureChangeSpeed * Time.fixedDeltaTime
        );

        StandingCollider.center = Vector3.Lerp(
            StandingCollider.center,
            _targetColliderCenter,
            PostureChangeSpeed * Time.fixedDeltaTime
        );
    }

    private void UpdateStamina(bool isSprinting)
    {
        if (isSprinting)
        {
            float useAmount = SprintStaminaUsePerSecond * Time.fixedDeltaTime;

            _playerStatus.UseStamina(useAmount);

            if (_playerStatus.Model.CurrentStamina <= 0f)
            {
                _isSprintExhausted = true;
            }
            return;
        }

        float recoverAmount = StaminaRecoveryPerSecond * Time.fixedDeltaTime;
        _playerStatus.RecoverStamina(recoverAmount);
    }

    private void UpdateSprintExhaustion()
    {
        if (!_isSprintExhausted)
            return;

        float restartStamina = _playerStatus.Model.MaxStamina * SprintRestartStaminaRatio;

        if (_playerStatus.Model.CurrentStamina >= restartStamina)
        {
            _isSprintExhausted = false;
        }
    }
}
