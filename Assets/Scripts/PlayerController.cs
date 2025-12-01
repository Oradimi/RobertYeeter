using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float chargeBreak = 0.95f;
    
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private AudioClip splashSound;
    [SerializeField] private AudioClip cantMoveSound;

    [SceneLabel(fontSize: 32)]
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private string _prompt;
#pragma warning restore CS0414 // Field is assigned but its value is never used

    private float _promptTime;
    private float _direction;
    
    private Animator _animator;
    private static readonly int ChargeTrigger = Animator.StringToHash("Charge");
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");
    private static readonly int DrownTrigger = Animator.StringToHash("Drown");
    private static readonly int CaughtTrigger = Animator.StringToHash("Caught");
    private static readonly int WetBool = Animator.StringToHash("Wet");
    
    private AudioSource _audioSource;
    private PlayerControls _controls;
    
    private Vector3 _targetPosition;
    private Vector3 _chargePosition;
    [SceneLabel(SceneLabelID.Cooldown)]
    private float _chargeCooldown;
    private float _initialChargeBreak;
    
    private bool _isCharging;
    private bool _isJumping;
    private bool _isFalling;
    private float _jumpTime;
    private bool _inWater;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
        _prompt = "";
        _promptTime = 1f;
        _controls = new PlayerControls();
        _targetPosition = Vector3.right;
        _chargePosition = Vector3.zero;
        _initialChargeBreak = chargeBreak;
    }

    private void OnEnable()
    {
        _audioSource.mute = UnlocksManager.soundEffectsMute;
        SceneLabelOverlay.OnSetSpecialAttribute += PlayerControllerLabelEffect;
        _animator.SetBool(WetBool, false);
        _controls.Enable();
        EnableActionMap();
        _controls.UI.Fullscreen.performed += OnFullscreen;
    }

    private void OnDisable()
    {
        DisableActionMap();
        DisableUIMap();
        _controls.UI.Fullscreen.performed -= OnFullscreen;
        _controls.Disable();
        SceneLabelOverlay.OnSetSpecialAttribute -= PlayerControllerLabelEffect;
    }

    private void FixedUpdate()
    {
        _animator.speed = GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f;

        if (FloorManager.IsPaused())
            return;

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition + _chargePosition, 
            Speed() * Time.fixedDeltaTime);
        
        CheckForJumpPrompt();
        CheckCollision();

        _chargePosition *= chargeBreak + (1f - chargeBreak) * (1f - GameManager.GlobalSpeed);
        _isCharging = _chargePosition.z < -0.2f;
        
        if (FloorManager.IsGameOver())
            return;
        
        _jumpTime -= Time.fixedDeltaTime * GameManager.GlobalSpeed;
        _chargeCooldown -= Time.fixedDeltaTime * GameManager.GlobalSpeed;
        _isJumping = _jumpTime > 0.4f;
        _isFalling = _jumpTime > 0.2f && !_isJumping;

        var positionNoZ = new Vector3(transform.position.x, transform.position.y, 0f);
        if (Vector3.Distance(positionNoZ, _targetPosition) < 0.1f)
            _direction = 0f;
    }

    private bool _collision1 = false;
    private bool _collision2 = false;
    
    private void CheckCollision()
    {
        if (_inWater || FloorManager.IsGameOver())
            return;
        
        var ray = new Ray(transform.position + Vector3.up, Vector3.down);
        Physics.Raycast(ray, out var hit, 1.2f, LayerMask.GetMask("Default"));
        if (_isJumping)
        {
            if (hit.collider && Mathf.Abs(hit.point.y - transform.position.y) < 0.2f)
                _targetPosition = new Vector3(_targetPosition.x, hit.point.y, _targetPosition.z);
        }
        else if (hit.collider)
        {
            _targetPosition = new Vector3(_targetPosition.x, hit.point.y, _targetPosition.z);
        }
        else if (!Physics.CheckSphere(transform.position + Vector3.up * 0.1f, 0.2f, LayerMask.GetMask("Default")))
        {
            _targetPosition += Vector3.down * (Time.fixedDeltaTime * Speed());
        }

        var frontCollider = transform.position + new Vector3(0f, 1.15f, -0.4f);
        _collision1 = Physics.CheckSphere(frontCollider, 0.3f, LayerMask.GetMask("Default"));
        if (Physics.CheckSphere(frontCollider, 0.3f, LayerMask.GetMask("Default")))
        {
            _chargePosition = new Vector3(0f, 0f, Mathf.Max(_chargePosition.z, 0f));
            _chargePosition += Vector3.forward * (FloorManager.GetFloorScrollSpeed() * Time.fixedDeltaTime);
            chargeBreak = 1f;
            if (_chargePosition.z < 2f)
                return;
            PlayPunchSound();
            SetCaught();
            GameManager.GameOver(GameManager.GameOverCase.Caught);
        }
        else
        {
            chargeBreak = _initialChargeBreak;
        }
    }

    private void OnDrawGizmos()
    {
        var roundedHeightTarget = transform.position + new Vector3(0f, 1.15f, -0.4f);
        Gizmos.color = _collision1 ? Color.red : Color.green;
        Gizmos.DrawSphere(roundedHeightTarget, 0.1f);
        var awa = _targetPosition + new Vector3(0.4f * _direction, 0.5f, -0.1f);
        Gizmos.color = _collision2 ? Color.red : Color.green;
        Gizmos.DrawSphere(awa, 0.3f);
    }

    public void EnableActionMap()
    {
        _controls.Player.Move.performed += OnMove;
        _controls.Player.Attack.performed += OnCharge;
        _controls.Player.Jump.performed += OnJump;
    }

    public void EnableUIMap()
    {
        _controls.UI.Pause.performed += OnPause;
    }

    public void DisableActionMap()
    {
        _controls.Player.Move.performed -= OnMove;
        _controls.Player.Attack.performed -= OnCharge;
        _controls.Player.Jump.performed -= OnJump;
    }

    public void DisableUIMap()
    {
        _controls.UI.Pause.performed -= OnPause;
    }

    public bool IsCharging()
    {
        return _isCharging;
    }

    public void SetCaught()
    {
        _animator.SetTrigger(CaughtTrigger);
    }

    public void PlayPunchSound()
    {
        _audioSource.PlayOneShot(punchSound);
    }

    public float Speed()
    {
        return speed * (GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f);
    }

    private void CheckForJumpPrompt()
    {
        if (!TargetingWater() || _inWater || _isJumping)
        {
            GameManager.GlobalSpeed = 1f;
            GameManager.AffectsAnimations = true;
            _promptTime = 1f;
            _prompt = "";
            return;
        }
        
        GameManager.GlobalSpeed = 0.1f;
        GameManager.AffectsAnimations = true;
        _promptTime -= Time.fixedDeltaTime;
        _prompt = "Jump!";
        
        if (_promptTime < 0f || transform.position.y < -0.1f)
        {
            _inWater = true;
            _targetPosition += Vector3.down * 0.85f;
            _animator.SetBool(WetBool, true);
            _animator.SetTrigger(DrownTrigger);
            _audioSource.PlayOneShot(splashSound);
            GameManager.GameOver(GameManager.GameOverCase.Drowned);
        }
    }
    
    private void OnMove(InputAction.CallbackContext ctx)
    {
        var input = ctx.ReadValue<Vector2>();
        var newDirection = Mathf.Round(-input.x);

        if (TargetingWater() && Mathf.Approximately(newDirection, _direction))
            return;
        
        _direction = newDirection;

        _collision2 = Physics.CheckSphere(CombinedTargetPositions() + new Vector3(0.4f * _direction, 0.5f, -0.1f), 0.3f,
            LayerMask.GetMask("Default"));
        if (Physics.CheckSphere(CombinedTargetPositions() + new Vector3(0.4f * _direction, 0.5f, -0.1f), 0.3f,
                LayerMask.GetMask("Default")))
        {
            if (Physics.CheckSphere(transform.position + new Vector3(0.4f * _direction, 0.5f, -0.1f), 0.3f,
                    LayerMask.GetMask("Default")))
                _audioSource.PlayOneShot(cantMoveSound);
            return;
        }
        _targetPosition = new Vector3(Mathf.Clamp(_targetPosition.x + _direction, -3.0f, 3.0f), _targetPosition.y, _targetPosition.z);
    }

    private Vector3 CombinedTargetPositions()
    {
        return _targetPosition + _chargePosition;
    }

    private bool TargetingWater()
    {
        var roundedHeightTarget = new Vector3(DirectionCeil(transform.position.x) + _direction * 0.65f, Mathf.Max(transform.position.y, 0f), transform.position.z);
        if (Physics.CheckSphere(roundedHeightTarget, 0.1f, LayerMask.GetMask("Default")))
            return false;
        var ray = new Ray(roundedHeightTarget, Vector3.down);
        if (Physics.Raycast(ray, out var hit, 0.5f, LayerMask.GetMask("Default")))
            return hit.normal == Vector3.up;
        return false;
    }

    private void OnCharge(InputAction.CallbackContext ctx)
    {
        if (_isCharging || _chargeCooldown > 0f || _chargePosition.z > 0.01f)
        {
            _audioSource.PlayOneShot(cantMoveSound);
            return;
        }
        
        _isCharging = true;
        _animator.SetTrigger(ChargeTrigger);
        _chargePosition += new Vector3(0, 0, -2);
        _chargeCooldown = 2f;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_isJumping || _isFalling)
        {
            _audioSource.PlayOneShot(cantMoveSound);
            return;
        }
        
        _animator.SetTrigger(JumpTrigger);
        _jumpTime = 1f;
    }

    private void OnPause(InputAction.CallbackContext ctx)
    {
        FloorManager.TogglePause();
    }

    private void OnFullscreen(InputAction.CallbackContext ctx)
    {
        Screen.fullScreen = !Screen.fullScreen;
    }

    private void PlayerControllerLabelEffect(SceneLabelAttribute attr, SceneLabelOverlay.SceneLabelOverlayData data)
    {
        if (attr.ID != SceneLabelID.Cooldown)
            return;
        attr.Value = _chargeCooldown > 0 ? $"{_chargeCooldown:F2}" : "";
        attr.RichValue = !_isCharging && _chargeCooldown > 0 ? $"<color=red>{_chargeCooldown:F2}</color>" : null;
    }

    private float DirectionCeil(float value)
    {
        return _direction > 0 ? Mathf.Floor(value) : Mathf.Ceil(value);
    }

    public bool ToggleMuteSoundEffects()
    {
        UnlocksManager.soundEffectsMute ^= true;
        _audioSource.mute ^= true;
        return _audioSource.mute;
    }

    public bool IsMuteSoundEffects()
    {
        return _audioSource.mute;
    }
}
