using UnityEngine;

public enum StatusEffectType
{
    None,
    Bleed,
    Poison,
    Stun,
    MoveFast,
    MoveSlow,
    ActFast,
    ActSlow,
}

public interface IStatusEffectable : IDamageable
{
    void SetSpeedMultiplier( float multiplier ); // 속도 배율 적용
    void SetStunned( bool isStunned );           // 기절 상태 설정
}

public class StatusEffects
{
    private StatusEffectType _statusType;
    private float _value;
    private float _duration;
    private float _timer;
    private float _tickInterval;
    private float _tickTimer;

    public StatusEffects( StatusEffectType statusType, float value, float duration, float tickInterval = 1f )
    {
        _statusType = statusType;
        _value = value;
        _duration = duration;
        _tickInterval = tickInterval;
        _timer = 0f;
        _tickTimer = 0f;
    }

    public StatusEffectType GetStatusType( ) => _statusType;
    public float GetValue( ) => _value;

    // 지속 시간 갱신
    public bool UpdateDuration( float deltaTime )
    {
        _timer += deltaTime;
        return _timer >= _duration;
    }

    // 동일 상태 이상 재부여 시 타이머 리셋 (시간 갱신)
    public void RefreshDuration( float newDuration )
    {
        _duration = newDuration;
        _timer = 0f;
    }

    // DoT 데미지 타이머 체크
    public bool ShouldTick( float deltaTime )
    {
        if (_tickInterval <= 0f)
        {
            return false;
        }

        _tickTimer += deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            return true;
        }
        return false;
    }

    // DoT 여부 판단
    public bool IsDoTEffect( )
    {
        return _statusType == StatusEffectType.Bleed || _statusType == StatusEffectType.Poison;
    }

    // 이동 속도 변화량 계산
    public float GetSpeedModifier( )
    {
        if (_statusType == StatusEffectType.MoveFast) return _value;
        if (_statusType == StatusEffectType.MoveSlow) return -_value;
        return 0f;
    }

    // 스턴 여부 판단
    public bool IsStunEffect( )
    {
        return _statusType == StatusEffectType.Stun;
    }
}