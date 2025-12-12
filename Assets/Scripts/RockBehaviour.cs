using System;
using UnityEngine;

public class RockBehaviour : MonoBehaviour
{
    private Animator _animator;
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");
    private static readonly int ChargeTrigger = Animator.StringToHash("Charge");

    private Vector3 _targetPosition;
    private Vector3 _chargePosition;
    private Vector3 _previousPosition;
    private bool _isCharging;
    private float _chargeCooldown;
    private float _chargeBreak;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _chargeBreak = 0.95f;
    }

    private void FixedUpdate()
    {
        _animator.speed = GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f;

        if (FloorManager.IsPaused())
            return;
        
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition + _chargePosition, 
            GameManager.GetPlayer().Speed() * Time.fixedDeltaTime);
        
        _chargePosition *= _chargeBreak + (1f - _chargeBreak) * (1f - GameManager.GlobalSpeed);
        _isCharging = _previousPosition.z > transform.position.z && _chargeCooldown >= 1f && _chargePosition.z < -1f;
        _chargeCooldown -= Time.fixedDeltaTime * GameManager.GlobalSpeed;

        CheckGroundCollision();

        _previousPosition = transform.position;
        
        if (_isCharging)
            return;

        var ray = new Ray(transform.position + Vector3.up * 0.1f + Vector3.back, Vector3.back);
        Physics.Raycast(ray, out var hit, 5f, LayerMask.GetMask("Robert"));
        if (hit.collider)
            Charge(hit.transform.position.z);
    }

    private void OnDrawGizmos()
    {
        var ray = new Ray(transform.position + Vector3.up * 0.1f + Vector3.back, Vector3.back);
        Gizmos.DrawRay(ray);
    }

    public void SetTargetPosition(Vector3 position, bool keepY)
    {
        _targetPosition = keepY ? new Vector3(position.x, _targetPosition.y, position.z) : position;
    }

    public void Jump()
    {
        _animator.SetTrigger(JumpTrigger);
    }

    public void Charge(float position)
    {
        _animator.SetTrigger(ChargeTrigger);
        _chargePosition += new Vector3(0f, 0f, 2f * position);
        _chargeCooldown = 4f;
    }

    public void StopCharge()
    {
        _chargePosition = transform.position + Vector3.forward;
    }

    public bool IsCharging()
    {
        return _isCharging;
    }

    private void CheckGroundCollision()
    {
        var ray = new Ray(transform.position + Vector3.up, Vector3.down);
        Physics.Raycast(ray, out var hit, 4f, LayerMask.GetMask("Default"));
        if (hit.collider)
            _targetPosition = new Vector3(_targetPosition.x, hit.point.y, _targetPosition.z);
    }
}