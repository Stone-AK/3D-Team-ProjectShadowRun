using UnityEngine;

public class TestPlayer : MonoBehaviour,IDamageable,IBattleAgent
{
    BattleAgentTeamType _battleAgentTeamType = BattleAgentTeamType.Player;
    public void TakeDamage(float damage) { }//DamageInfo구조체를 만들어 전달하면 더 많은 정보를 전달할수 있음

    public BattleAgentTeamType Team { get => _battleAgentTeamType; }
    public Transform Transform { get => this.transform; }

    public bool IsDead { get; }
    public void UseWeapon() { }//무기를 사용할때 공격자를 전달할수있음

   
}
