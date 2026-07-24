using System.Collections;
using UnityEngine;

public class ActivateMedicine : MonoBehaviour, IQuickSlotConsumeHandler
{
    private Coroutine _regenCoroutine;

    private void OnDisable( )
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = null;
        }
    }

    public bool CanHandleType( ItemData itemData )
    {
        if (itemData == null)
        {
            return false;
        }

        // ItemType이 Consumable이면서 UseItemType이 맞는 경우만 처리
        bool isConsumable = itemData.ItemType == "Consumable";
        bool isValidUseType = itemData.UseItemType == "HealStat" ||
                             itemData.UseItemType == "BuffStat";

        return isConsumable && isValidUseType;
    }

    public void UseItem( ItemData itemData )
    {
        if (itemData == null)
        {
            Debug.LogError("ItemData is null."); return;
        }

        // 즉시 회복 처리
        if (itemData.UseItemType == "HealStat")
        {
            if (itemData.TryGetParameter("HealAmount", out float healAmount))
            {
                if (PlayerStatus.Instance != null)
                {
                    PlayerStatus.Instance.RecoverHP(healAmount);
                }
            }
        }
        // 지속형(버프) 처리
        else if (itemData.UseItemType == "BuffStat")
        {
            ApplyBuff(itemData);
        }
    }

    private void ApplyBuff( ItemData itemData )
    {
        // 공통 지속시간 가져오기 (없으면 기본 60초)
        if (!itemData.TryGetParameter("Duration", out float duration) || duration <= 0f)
        {
            duration = 60f;
        }

        // 지속 체력 회복 (RegenHP)
        if (itemData.TryGetParameter("RegenHP", out float totalRegen) && totalRegen > 0f)
        {
            if (_regenCoroutine != null)
            {
                StopCoroutine(_regenCoroutine);
            }
            _regenCoroutine = StartCoroutine(RegenRoutine(totalRegen, duration));
        }

        if (itemData.TryGetParameter("IgnorePain", out float reducePain))
        {
            if (PlayerStatus.Instance != null)
            {
                // Todo: 진통제 사용으로 300만큼의 고통 감소 효과를 적용하는 로직을 구현해야 합니다.
            }
        }

        if (itemData.TryGetParameter("SpeedBoost", out float speedBoost))
        {
            if (PlayerStatus.Instance != null)
            {
                //Todo: 속도 증가 효과 60을 적용하는 로직을 구현해야 합니다.
            }
        }

    }

    private IEnumerator RegenRoutine( float totalRegen, float duration )
    {
         float timer = 0f;

         while (timer < duration)
         {
            timer += Time.deltaTime;
            float tickHeal = ( totalRegen / duration ) * Time.deltaTime;

            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.RecoverHP(tickHeal);
            }

            yield return null;
         }

        _regenCoroutine = null;
    }
}
