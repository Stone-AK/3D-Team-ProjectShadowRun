using System.Collections.Generic;
using UnityEngine;

public class StatusConditionManager : MonoBehaviour
{
    private static StatusConditionManager _instance;

    public static StatusConditionManager Instance
    {
        get
        {
            return _instance;
        }
    }

    private Dictionary<IDamageable, List<StatusCondition>> _targetCondition = new Dictionary<IDamageable, List<StatusCondition>>();

    private void Awake( )
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update( )
    {
        UpdateAllEffects();
    }

    // 대상에게 상태 이상 부여
    public void ApplyEffect( IDamageable target, StatusConditionType type, float value, float duration, float tickInterval = 1f )
    {
        if (target == null)
        {
            return;
        }

        if (!_targetCondition.ContainsKey(target))
        {
            _targetCondition.Add(target, new List<StatusCondition>());
        }

        StatusCondition newCondition = new StatusCondition(type, value, duration, tickInterval);
        _targetCondition[target].Add(newCondition);
    }

    // 모든 대상의 상태 이상 매 프레임 갱신
    private void UpdateAllEffects( )
    {
        List<IDamageable> keysToRemove = new List<IDamageable>();

        foreach (KeyValuePair<IDamageable, List<StatusCondition>> pair in _targetCondition)
        {
            IDamageable target = pair.Key;
            List<StatusCondition> conditionList = pair.Value;

            MonoBehaviour targetObj = target as MonoBehaviour;

            // 오브젝트가 Destroy되었거나, 비활성화(오브젝트 풀 반환/사망)된 경우
            if (targetObj == null || targetObj.gameObject.activeInHierarchy == false)
            {
                keysToRemove.Add(target);
                continue;
            }

            for (int i = conditionList.Count - 1; i >= 0; i--)
            {
                StatusCondition condition = conditionList[i];

                // 지속 데미지 계열만 TakeDamage 호출
                if (IsDoTEffect(condition.GetConditionType()))
                {
                    if (condition.ShouldTick(Time.deltaTime))
                    {
                        target.TakeDamage(condition.GetValue());
                    }
                }

                // 지속 시간 만료 체크
                if (condition.UpdateDuration(Time.deltaTime))
                {
                    conditionList.RemoveAt(i);
                }
            }
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            _targetCondition.Remove(keysToRemove[i]);
        }
    }

    // DoT 데미지 판별
    private bool IsDoTEffect( StatusConditionType type )
    {
        return type == StatusConditionType.Bleed || type == StatusConditionType.Poison;
    }

    // 이동 속도 배율 계산 (Enemy / Player가 이동 로직에서 곱해서 사용)
    public float GetMoveSpeedMultiplier( IDamageable target )
    {
        if (target == null || !_targetCondition.ContainsKey(target))
        {
            return 1f;
        }

        float multiplier = 1f;
        List<StatusCondition> list = _targetCondition[target];

        for (int i = 0; i < list.Count; i++)
        {
            StatusConditionType type = list[i].GetConditionType();

            if (type == StatusConditionType.MoveFast)
            {
                multiplier += list[i].GetValue(); // 예: 0.2f면 +20%
            }
            else if (type == StatusConditionType.MoveSlow || type == StatusConditionType.Slow)
            {
                multiplier -= list[i].GetValue(); // 예: 0.3f면 -30%
            }
        }

        return Mathf.Max(0.1f, multiplier); // 최소 10% 속도 보장
    }

    // 특정 상태 이상(스턴 등) 포함 여부 확인
    public bool HasCondition( IDamageable target, StatusConditionType type )
    {
        if (target == null || !_targetCondition.ContainsKey(target))
        {
            return false;
        }

        List<StatusCondition> list = _targetCondition[target];
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].GetConditionType() == type)
            {
                return true;
            }
        }

        return false;
    }



    // 사망 시 또는 풀링 반환 시 상태 이상 정리
    public void ClearEffects( IDamageable target )
    {
        if (target != null && _targetCondition.ContainsKey(target))
        {
            _targetCondition.Remove(target);
        }
    }
}