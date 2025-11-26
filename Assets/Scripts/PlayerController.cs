using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
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
        _promptTime = 100f;
        _controls = new PlayerControls();
        _targetPosition = Vector3.right;
        _chargePosition = Vector3.zero;
    }

    private void OnEnable()
    {
        _animator.SetBool(WetBool, false);
        _controls.Enable();
        _controls.Player.Move.performed += OnMove;
        _controls.Player.Attack.performed += OnCharge;
        _controls.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        DisableActionMap();
    }

    private void FixedUpdate()
    {
        _animator.speed = GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f;

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition + _chargePosition, 
            Speed() * Time.fixedDeltaTime);
        
        CheckForJumpPrompt();
        CheckCollision();
        
        _chargePosition *= chargeBreak;
        _isCharging = _chargePosition.z < -0.2f;
        
        _jumpTime -= Time.fixedDeltaTime;
        _isJumping = _jumpTime > 0.4f;
        _isFalling = _jumpTime > 0f && !_isJumping;
    }

    private bool _collision1 = false;
    private bool _collision2 = false;
    private bool _collision3 = false;
    
    private void CheckCollision()
    {
        if (_inWater || FloorManager.IsGameOver())
            return;
        
        var ray = new Ray(transform.position + Vector3.up, Vector3.down);
        Physics.Raycast(ray, out var hit, 1.2f, LayerMask.GetMask("Default"));
        if (hit.collider)
        {
            _targetPosition = new Vector3(_targetPosition.x, hit.point.y, _targetPosition.z);
        }
        else
        {
            _collision2 = Physics.CheckSphere(transform.position, 0.2f, LayerMask.GetMask("Default"));
            if (!Physics.CheckSphere(transform.position + Vector3.up * 0.1f, 0.2f, LayerMask.GetMask("Default")))
            {
                _targetPosition += Vector3.down * (Time.fixedDeltaTime * speed);
                _targetPosition = new Vector3(_targetPosition.x, Mathf.Max(_targetPosition.y, 0f), _targetPosition.z);
                if (TargetingWater())
                    _promptTime = -1f;
            }
        }
        
        _collision3 = Physics.CheckSphere(transform.position + new Vector3(0f, 0.9f, -0.5f), 0.2f,
            LayerMask.GetMask("Default"));
        if (Physics.CheckSphere(transform.position + new Vector3(0f, 0.9f, -0.5f), 0.2f, LayerMask.GetMask("Default")))
        {
            PlayPunchSound();
            SetCaught();
            GameManager.GameOver(GameManager.GameOverCase.Caught);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _collision1 ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position + new Vector3(0.4f * _direction, 0.5f, -0.5f), 0.3f);
        Gizmos.color = _collision2 ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f, 0.2f);
        Gizmos.color = _collision3 ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position + new Vector3(0f, 0.9f, -0.5f), 0.2f);
    }

    public void DisableActionMap()
    {
        _controls.Player.Move.performed -= OnMove;
        _controls.Player.Attack.performed -= OnCharge;
        _controls.Player.Jump.performed -= OnJump;
        _controls.Disable();
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

    private float Speed()
    {
        return speed * (GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f);
    }

    private void CheckForJumpPrompt()
    {
        if (!TargetingWater() || _inWater)
            return;
        
        if (_promptTime < 0f)
        {
            _inWater = true;
            _prompt = "";
            _targetPosition = Vector3.down * 1f;
            _animator.SetBool(WetBool, true);
            _animator.SetTrigger(DrownTrigger);
            _audioSource.PlayOneShot(splashSound);
            GameManager.GameOver(GameManager.GameOverCase.Drowned);
            return;
        }
        
        GameManager.GlobalSpeed = 0.1f;
        GameManager.AffectsAnimations = true;
        _promptTime -= Time.deltaTime;
        _prompt = "Jump!";

        if (_isJumping)
        {
            GameManager.GlobalSpeed = 1f;
            GameManager.AffectsAnimations = true;
            _promptTime = 100f;
            _prompt = "";
            _targetPosition = new Vector3(Mathf.Clamp(_targetPosition.x + _direction, -3.0f, 3.0f), _targetPosition.y, _targetPosition.z);
        }
    }
    
    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (TargetingWater())
            return;
        
        var input = ctx.ReadValue<Vector2>();
        _direction = Mathf.Round(-input.x);
        _collision1 = Physics.CheckSphere(transform.position + new Vector3(0.4f * _direction, 0.5f, -0.5f), 0.3f,
            LayerMask.GetMask("Default"));
        if (Physics.CheckSphere(transform.position + new Vector3(0.4f * _direction, 0.5f, -0.5f), 0.3f, LayerMask.GetMask("Default")))
        {
            _audioSource.PlayOneShot(cantMoveSound);
            return;
        }
        
        _targetPosition = new Vector3(Mathf.Clamp(_targetPosition.x + _direction, -3.0f, 3.0f), _targetPosition.y, _targetPosition.z);
        if (TargetingWater())
            _promptTime = 1f;
    }

    private bool TargetingWater()
    {
        return _targetPosition.x == 0f && _targetPosition.y < 1f;
    }

    private void OnCharge(InputAction.CallbackContext ctx)
    {
        if (_isCharging)
            return;
        _animator.SetTrigger(ChargeTrigger);
        _chargePosition = new Vector3(0, 0, -2);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_isJumping || _isFalling)
            return;
        _isCharging = false;
        _animator.SetTrigger(JumpTrigger);
        _jumpTime = 1f;
    }
}
