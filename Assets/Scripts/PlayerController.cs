using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    
    private PlayerControls _controls;
    private Vector3 _targetPosition;
    
    private void Awake()
    {
        _controls = new PlayerControls();
        _targetPosition = new Vector3(1, 0, 0);
    }

    private void OnEnable()
    {
        _controls.Enable();
        _controls.Player.Move.performed += OnMove;
    }

    private void OnDisable()
    {
        _controls.Player.Move.performed -= OnMove;
        _controls.Disable();
    }

    private void FixedUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, speed * Time.fixedDeltaTime);
    }
    
    private void OnMove(InputAction.CallbackContext ctx)
    {
        var input = ctx.ReadValue<Vector2>();
        _targetPosition = new Vector3(Mathf.Clamp(_targetPosition.x - input.x, -2.0f, 2.0f), 0, 0);
    }
}
