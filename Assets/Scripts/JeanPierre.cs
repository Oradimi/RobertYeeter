using UnityEngine;

public class JeanPierre : MonoBehaviour
{
    [SceneLabel(SceneLabelID.EnemyName, r: 1, g: 0, b: 0)]
    [SerializeField] private string robertName = "Jean-Pierre";
    
    private Animator _animator;
    private Vector3 _targetPosition;
    private bool _yeeted;
    private bool _playerCaught;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        _animator.speed = (GameManager.AffectsAnimations ? GameManager.GlobalSpeed : 1f) * 2f;
        
        if (_yeeted)
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, 10f * Time.deltaTime);
        
        if (Physics.CheckSphere(transform.position, 0.5f, LayerMask.GetMask("Player")))
            PlayerTouched();
    }
    
    public void SetRobertName(string newName)
    {
        robertName = newName;
        _targetPosition = Vector3.zero;
    }

    private void PlayerTouched()
    {
        if (_yeeted || _playerCaught)
            return;

        _playerCaught = true;
        GameManager.GetPlayer().PlayPunchSound();
        GameManager.GetPlayer().SetCaught();
        GameManager.GameOver(GameManager.GameOverCase.Caught);
    }
}
