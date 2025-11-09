using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float chargeBreak = 0.95f;
    
    private Animator _animator;
    private static readonly int ChargeTrigger = Animator.StringToHash("Charge");
    
    private PlayerControls _controls;
    private Vector3 _targetPosition;
    private Vector3 _chargePosition;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controls = new PlayerControls();
        _targetPosition = Vector3.right;
        _chargePosition = Vector3.zero;
    }

    private void OnEnable()
    {
        _controls.Enable();
        _controls.Player.Move.performed += OnMove;
        _controls.Player.Attack.performed += OnCharge;
    }

    private void OnDisable()
    {
        _controls.Player.Move.performed -= OnMove;
        _controls.Disable();
    }

    private void FixedUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition + _chargePosition,
            speed * Time.fixedDeltaTime);
        _chargePosition *= chargeBreak;
    }
    
    private void OnMove(InputAction.CallbackContext ctx)
    {
        var input = ctx.ReadValue<Vector2>();
        _targetPosition = new Vector3(Mathf.Clamp(_targetPosition.x - Mathf.Round(input.x), -2.0f, 2.0f), 0, 0);
    }

    private void OnCharge(InputAction.CallbackContext ctx)
    {
        var isCharging = _chargePosition.z < -0.2f;
        if (isCharging)
            return;
        _animator.SetTrigger(ChargeTrigger);
        _chargePosition = new Vector3(0, 0, -2);
    }
}
