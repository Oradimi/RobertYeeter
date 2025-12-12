using SceneLabel;
using UnityEngine;

public class RobertBehaviour : MonoBehaviour
{
    [SceneLabel(SceneLabelID.EnemyName, fontSize: 12)]
#pragma warning disable 0414
    [SerializeField] private string robertName = "Robert";
#pragma warning restore 0414
    [SerializeField] private int healthPoints = 3;
    [SerializeField] private bool invincible;
    
    private Animator _animator;
    private Vector3 _targetPosition;
    private bool _yeeted;
    private bool _playerCaught;
    private Collider[] _hitResults; 

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _hitResults =  new Collider[8];
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

        var size = Physics.OverlapSphereNonAlloc(transform.position, 0.3f, _hitResults, LayerMask.GetMask("Rock"));
        if (size > 0)
            RockTouched(_hitResults[0].GetComponent<RockBehaviour>());
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
    
    private void RockTouched(RockBehaviour rock)
    {
        if (_yeeted || rock.IsCharging())
            return;
        
        rock.StopCharge();
        
        if (healthPoints > 0)
        {
            healthPoints -= 1;
            GameManager.GetPlayer().PlayLightPunchSound();
        }
        else
        {
            _yeeted = true;
            GameManager.GetPlayer().PlayPunchSound();
            GameManager.AddScore(1);
            _targetPosition = Vector3.up * 100f;
            Destroy(gameObject, 3f);
        }

        // var results = new Collider[10];
        // var size = Physics.OverlapSphereNonAlloc(transform.position, 0.15f, results, LayerMask.GetMask("Rock"));
        //
        // for (var i = 0; i < size; i++)
        //     Destroy(results[i].gameObject);
    }
}
