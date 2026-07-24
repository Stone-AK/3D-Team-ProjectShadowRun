using UnityEngine;

public enum StatusConditionType
{
    None,
    Bleed,
    Poison,
    Slow,
    Stun,
    MoveFast,
    MoveSlow,
    ActFast,    
    ActSlow,
}

public class StatusCondition
{
    private StatusConditionType _ConditionType;
    private float _value;
    private float _duration;
    private float _timer;
    private float _tickInterval;
    private float _tickTimer;

    public StatusCondition( StatusConditionType conditionType, float value, float duration, float tickInterval = 1f )
    {
        _ConditionType = conditionType;
        _value = value;
        _duration = duration;
        _tickInterval = tickInterval;
        _timer = 0f;
        _tickTimer = 0f;
    }

    public StatusConditionType GetConditionType( )
    {
        return _ConditionType;
    }

    public float GetValue( )
    {
        return _value;
    }

    public bool UpdateDuration( float deltaTime )
    {
        _timer += deltaTime;
        return _timer >= _duration;
    }

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
}