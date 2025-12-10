using SceneLabel;
using UnityEngine;

public class RobertBehaviour : MonoBehaviour
{
    [SceneLabel(SceneLabelID.EnemyName, fontSize: 12)]
#pragma warning disable 0414
    [SerializeField] private string robertName = "Robert";
#pragma warning restore 0414
    [SerializeField] private bool invincible;
    
    private Animator _animator;
    private Vector3 _targetPosition;
    private bool _yeeted;
    private bool _playerCaught;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        SceneLabelOverlay.OnSetSpecialAttribute += GameManager.EnemyNameDisplayLabelEffect;
    }
    
    private void OnDisable()
    {
        SceneLabelOverlay.OnSetSpecialAttribute -= GameManager.EnemyNameDisplayLabelEffect;
    }

    private void FixedUpdate()
    {
        _animator.speed = GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f;
        
        if (_yeeted)
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, 10f * Time.fixedDeltaTime);
        
        if (Physics.CheckSphere(transform.position, 0.3f, LayerMask.GetMask("Player")))
            PlayerTouched();
        
        if (Physics.CheckSphere(transform.position, 0.3f, LayerMask.GetMask("Rock")))
            RockTouched();
    }

    private void PlayerTouched()
    {
        if (_yeeted || _playerCaught)
            return;
        
        if (!invincible && GameManager.GetPlayer().IsCharging())
        {
            _yeeted = true;
            GameManager.GetPlayer().PlayPunchSound();
            GameManager.AddScore(1);
            _targetPosition = Vector3.up * 100f;
            Destroy(gameObject, 3f);
            return;
        }

        _playerCaught = true;
        GameManager.GetPlayer().PlayPunchSound();
        GameManager.GetPlayer().SetCaught();
        GameManager.GameOver(GameManager.GameOverCase.Caught);
    }
    
    private void RockTouched()
    {
        if (_yeeted)
            return;
        
        if (!invincible)
        {
            _yeeted = true;
            GameManager.GetPlayer().PlayPunchSound();
            GameManager.AddScore(1);
            _targetPosition = Vector3.up * 100f;
            Destroy(gameObject, 3f);
            return;
        }

        var results = new Collider[10];
        var size = Physics.OverlapSphereNonAlloc(transform.position, 0.3f, results, LayerMask.GetMask("Rock"));

        for (var i = 0; i < size; i++)
            Destroy(results[i].gameObject);
    }
}
