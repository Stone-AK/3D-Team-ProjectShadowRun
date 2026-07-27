using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    private static StatusEffectManager _instance;

    public static StatusEffectManager Instance
    {
        get
        {
            return _instance;
        }
    }
    private Dictionary<IStatusEffectable, List<StatusEffects>> _targetStatusEffects = new Dictionary<IStatusEffectable, List<StatusEffects>>();

    // GC 발생 방지용 삭제 목록 리스트 캐싱
    private List<IStatusEffectable> _keysToRemove = new List<IStatusEffectable>();

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
    public void ApplyEffect( IStatusEffectable target, StatusEffectType type, float value, float duration, float tickInterval = 1f )
    {
        if (target == null) return;

        if (!_targetStatusEffects.ContainsKey(target))
        {
            _targetStatusEffects.Add(target, new List<StatusEffects>());
        }

        List<StatusEffects> list = _targetStatusEffects[target];

        // 이미 동일한 타입의 효과가 있다면 새로 추가하지 않고 시간만 갱신 (중복 덮어쓰기 방지)
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].GetStatusType() == type)
            {
                list[i].RefreshDuration(duration);
                return;
            }
        }

        StatusEffects newEffect = new StatusEffects(type, value, duration, tickInterval);
        list.Add(newEffect);
    }

    // 모든 대상의 상태 이상 일괄 관리
    private void UpdateAllEffects( )
    {
        _keysToRemove.Clear();

        // foreach의 KeyValuePair 타입도 IStatusEffectable로 동일하게 작성
        foreach (KeyValuePair<IStatusEffectable, List<StatusEffects>> pair in _targetStatusEffects)
        {
            IStatusEffectable target = pair.Key;
            List<StatusEffects> effectList = pair.Value;
            MonoBehaviour targetObj = target as MonoBehaviour;

            // 유니티 Destroy(Fake Null) 안전 검사 강화
            if (targetObj == null || targetObj.Equals(null) || targetObj.gameObject.activeInHierarchy == false)
            {
                _keysToRemove.Add(target);
                continue;
            }

            float currentSpeedMultiplier = 1f;
            bool isStunned = false;

            for (int i = effectList.Count - 1; i >= 0; i--)
            {
                StatusEffects effect = effectList[i];

                if (effect.IsDoTEffect() && effect.ShouldTick(Time.deltaTime))
                {
                    target.TakeDamage(effect.GetValue());
                }

                currentSpeedMultiplier += effect.GetSpeedModifier();
                if (effect.IsStunEffect())
                {
                    isStunned = true;
                }

                if (effect.UpdateDuration(Time.deltaTime))
                {
                    effectList.RemoveAt(i);
                }
            }

            target.SetSpeedMultiplier(Mathf.Max(0.1f, currentSpeedMultiplier));
            target.SetStunned(isStunned);

            if (effectList.Count == 0)
            {
                _keysToRemove.Add(target);
            }
        }

        // Dictionary 안전성 보장 후 리스트 정리
        for (int i = 0; i < _keysToRemove.Count; i++)
        {
            IStatusEffectable removeTarget = _keysToRemove[i];

            _targetStatusEffects.Remove(removeTarget);

            // 유니티 객체가 살아있을 때만 스탯을 안전하게 원복
            if (removeTarget != null && removeTarget is MonoBehaviour mono && mono != null && !mono.Equals(null))
            {
                removeTarget.SetSpeedMultiplier(1f);
                removeTarget.SetStunned(false);
            }
        }
    }

    // 대상의 최종 이동 속도 배율 계산
    public float GetMoveSpeedMultiplier( IStatusEffectable target )
    {
        if (target == null || !_targetStatusEffects.ContainsKey(target))
        {
            return 1f;
        }

        float multiplier = 1f;
        List<StatusEffects> list = _targetStatusEffects[target];

        for (int i = 0; i < list.Count; i++)
        {
            multiplier += list[i].GetSpeedModifier();
        }

        return Mathf.Max(0.1f, multiplier);
    }

    // 대상의 스턴 여부 확인
    public bool IsStunned( IStatusEffectable target )
    {
        if (target == null || !_targetStatusEffects.ContainsKey(target))
        {
            return false;
        }

        List<StatusEffects> list = _targetStatusEffects[target];

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].IsStunEffect())
            {
                return true;
            }
        }

        return false;
    }

    // 사망 시/풀링 반환 시 상태 정리
    public void ClearEffects( IStatusEffectable target )
    {
        if (target != null && _targetStatusEffects.ContainsKey(target))
        {
            _targetStatusEffects.Remove(target);
        }
    }
}