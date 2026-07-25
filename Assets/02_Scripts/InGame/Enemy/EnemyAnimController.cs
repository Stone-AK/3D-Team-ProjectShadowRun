using UnityEngine;

public enum EnemyAnimState 
{
    Idle,
    Move,
    Walk,
    SideWalk,
    Attack,
    Reload,
    Dead
}
public class EnemyAnimController : MonoBehaviour
{
    public Animator _animator;
    private void Awake()
    {
        _animator= GetComponent<Animator>();
    }
    public void ChangeAnimState(EnemyAnimState state) 
    {
        switch (state) 
        {
            case EnemyAnimState.Idle: break;
            case EnemyAnimState.Attack: _animator.SetBool("IsAttack",true); break;
            case EnemyAnimState.Reload: _animator.SetBool("IsReload",true); break;
            case EnemyAnimState.SideWalk: _animator.SetBool("IsSideWalk", true); break;
            case EnemyAnimState.Dead: _animator.SetTrigger("IsDead"); break;
        }
    }
    public void ChangeAnimState(EnemyAnimState state, float moveSpeed) 
    {
        _animator.SetFloat("MoveSpeed", moveSpeed);
    }
    public void ResetAnimState()
    {
        _animator.SetBool("IsReload", false);
        _animator.SetBool("IsAttack", false);
       // _animator.SetBool("IsDead", false);
    }
}
