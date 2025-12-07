using MagicaCloth2;
using SceneLabel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float chargeBreak = 0.95f;
    
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private AudioClip splashSound;
    [SerializeField] private AudioClip cantMoveSound;
    
    [SerializeField] private AnimationClip[] idleAnimations;
    [SerializeField] private AnimationClip[] happyIdleAnimations;

    [SerializeField] private MagicaCloth bunCloth;
    [SerializeField] private float looseningMultiplier = 2f;

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
    private static readonly int BlinkFloat = Animator.StringToHash("Blink");

    private AudioSource _audioSource;
    private PlayerControls _controls;

    private Vector3 _parentPosition;
    private Vector3 _targetPosition;
    private Vector3 _chargePosition;
    private float _chargeCooldown;
    private float _initialChargeBreak;

    private float _idleCooldown;
    private float _blinkTimer;
    private float _blinkTarget;
    private bool _isBlinking;
    
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
        _parentPosition = Vector3.zero;
        _targetPosition = Vector3.right;
        _chargePosition = Vector3.zero;
        _initialChargeBreak = chargeBreak;
        GameManager.SetPlayer(this);
    }

    private void OnEnable()
    {
        _animator.Play("Idle_0");
        _idleCooldown = idleAnimations[0].length;
        _animator.SetBool(WetBool, false);
        
        _audioSource.volume = GameManager.SoundEffectsVolume;
        _blinkTimer = 5f;
        GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(25, -80f);
        GameManager.ApplySkin();
        
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
    }

    private void FixedUpdate()
    {
        _animator.speed = GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f;
        
        ProcessBlinking();
        ProcessMagica();
        
        if (!FloorManager.IsStarted())
        {
            ProcessRandomIdle();
            return;
        }

        if (FloorManager.IsPaused())
            return;

        if (transform.parent)
            _parentPosition += Vector3.forward * (Time.fixedDeltaTime * 0.5f);

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition + _chargePosition + _parentPosition, 
            Speed() * Time.fixedDeltaTime);
        
        UIManager.UpdateStaminaBar(Mathf.Abs(_chargeCooldown - 1f));

        _chargePosition *= chargeBreak + (1f - chargeBreak) * (1f - GameManager.GlobalSpeed);
        _isCharging = _chargeCooldown >= 1f;
        
        CheckForJumpPrompt();
        
        if (FloorManager.IsGameOver())
            return;
        
        CheckCollision();
        
        _jumpTime -= Time.fixedDeltaTime * GameManager.GlobalSpeed;
        _chargeCooldown -= Time.fixedDeltaTime * GameManager.GlobalSpeed;
        _isJumping = _jumpTime > 0.4f;
        _isFalling = _jumpTime > 0.2f && !_isJumping;

        var positionNoZ = new Vector3(transform.position.x, transform.position.y, 0f);
        if (Vector3.Distance(positionNoZ, _targetPosition) < 0.1f)
            _direction = 0f;
    }

    private bool _collision1;
    private bool _collision2;
    
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
            GameManager.GameOver(GameManager.GameOverCase.Bonked);
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

    public void PlaySound(AudioClip clip)
    {
        _audioSource.PlayOneShot(clip);
    }

    public float Speed()
    {
        return speed * (GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f);
    }
    
    private void ProcessBlinking()
    {
        _blinkTimer -= Time.fixedDeltaTime;
        _animator.SetFloat(BlinkFloat, _blinkTarget, 0.1f - 0.05f * _blinkTarget, Time.fixedDeltaTime);

        if (!_isBlinking && _blinkTimer < 0f)
        {
            _isBlinking = true;
            CloseEyes();
        }
        else if (_isBlinking && _blinkTimer < 0f)
        {
            _isBlinking = false;
            OpenEyes();
        }
    }

    private void OpenEyes()
    {
        _blinkTarget = 0f;
        var quickBlinkMultiplier = Random.Range(0, 4) == 0 ? 0.5f : 1f;
        _blinkTimer = Random.Range(3f, 5f) * quickBlinkMultiplier;
    }

    private void CloseEyes()
    {
        _blinkTarget = _animator.GetBool(WetBool) ? 0.7f : 1f;
        _blinkTimer = Random.Range(0.20f, 0.25f);
    }

    private void ProcessRandomIdle()
    {
        _idleCooldown -= Time.fixedDeltaTime;

        while (_idleCooldown <= 0f)
        {
            int newIdleIndex;
            string newIdle;
            float newIdleCooldown;
            var happyIdle = Random.Range(0, 4) == 0;
            if (happyIdle)
            {
                OpenEyes();
                _blinkTimer += happyIdleAnimations.Length;
                newIdleIndex = Random.Range(0, happyIdleAnimations.Length);
                newIdle = "HappyIdle_" + newIdleIndex;
                newIdleCooldown = happyIdleAnimations[newIdleIndex].length;
            }
            else
            {
                newIdleIndex = Random.Range(0, idleAnimations.Length);
                newIdle = "Idle_" + newIdleIndex;
                newIdleCooldown = idleAnimations[newIdleIndex].length;
            }

            if (_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == newIdle)
                continue;

            _animator.CrossFadeInFixedTime(newIdle, 0.3f, 0);
            _idleCooldown = newIdleCooldown;
            break;
        }
    }

    private void ProcessMagica()
    {
        var bunClothData = bunCloth.SerializeData;
        bunClothData.angleRestorationConstraint.stiffness =
            new CurveSerializeData(Mathf.Exp(-0.001f * looseningMultiplier * GameManager.GetDistanceTraveled()));
        bunCloth.SetParameterChange();
    }

    private void CheckForJumpPrompt()
    {
        if (FloorManager.IsGameOver())
        {
            _promptTime = 1f;
            _prompt = "";
            return;
        }
        
        if (!TargetingWater() || _inWater || _isJumping)
        {
            GameManager.GlobalSpeed = 1f;
            GameManager.AffectsAnimations = false;
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
            transform.SetParent(FloorManager.GetCurrentFloor());
            GameManager.GameOver(GameManager.GameOverCase.Drowned);
        }
    }
    
    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (!FloorManager.IsStarted())
            return;
        
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
        if (IsMouseOverUi() || !FloorManager.IsStarted())
            return;
        
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

    public void PerformCharging()
    {
        _isCharging = true;
        _animator.CrossFadeInFixedTime("Run", 0.1f);
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

    private float DirectionCeil(float value)
    {
        return _direction > 0 ? Mathf.Floor(value) : Mathf.Ceil(value);
    }

    public void ChangeSoundEffectsVolume(float value)
    {
        _audioSource.volume = value * 0.01f;
        if (!_audioSource.isPlaying)
            _audioSource.PlayOneShot(cantMoveSound);
        GameManager.SoundEffectsVolume = _audioSource.volume;
    }
    
    private bool IsMouseOverUi()
    {
        if (!EventSystem.current)
            return false;
        
        var lastRaycastResult = ((InputSystemUIInputModule)EventSystem.current.currentInputModule).GetLastRaycastResult(Mouse.current.deviceId);
        const int uiLayer = 5;
        return lastRaycastResult.gameObject && lastRaycastResult.gameObject.layer == uiLayer;
    }
}
