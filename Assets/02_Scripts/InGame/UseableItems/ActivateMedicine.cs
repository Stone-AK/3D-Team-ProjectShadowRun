using System.Collections;
using UnityEngine;

public class ActivateMedicine : MonoBehaviour, IQuickSlotConsumeHandler
{
    private Coroutine _regenCoroutine;
    private Coroutine _speedCoroutine;
    private PlayerMovement _playerMovement;

    public event System.Action<float> DotHealBuffChanged;
    public event System.Action<float> SpeedBuffChanged;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnDisable( )
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = null;
        }

        if (_speedCoroutine != null)
        {
            StopCoroutine(_speedCoroutine);
            _speedCoroutine = null;
        }

        DotHealBuffChanged?.Invoke(0f);
        SpeedBuffChanged?.Invoke(0f);
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

        if (itemData.TryGetParameter("RegenHP", out float totalRegen) && totalRegen > 0f)
        {
            if (_regenCoroutine != null)
            {
                StopCoroutine(_regenCoroutine);
            }
            _regenCoroutine = StartCoroutine(RegenRoutine(totalRegen, duration));
        }

        if (itemData.TryGetParameter("IgnorePain", out float temporaryHP))
        {
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.AddTemporaryHP(temporaryHP);
            }
        }

        if (itemData.TryGetParameter("SpeedBoost", out float speedBoost))
        {
            if (_playerMovement == null || speedBoost <= 0f)
                return;

            _playerMovement.ApplySpeedBoost(speedBoost, duration);

            if (_speedCoroutine != null)
                StopCoroutine(_speedCoroutine);

            _speedCoroutine = StartCoroutine(SpeedBuffRoutine(duration));
        }

    }

    private IEnumerator RegenRoutine( float totalRegen, float duration )
    {
        float timer = 0f;
        int previousRemainTime = -1;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float tickHeal = ( totalRegen / duration ) * Time.deltaTime;

            if (PlayerStatus.Instance != null)
                PlayerStatus.Instance.RecoverHP(tickHeal);

            NotifyRemainTime(DotHealBuffChanged, duration - timer, ref previousRemainTime);
            yield return null;
        }

        DotHealBuffChanged?.Invoke(0f);
        _regenCoroutine = null;
    }

    private IEnumerator SpeedBuffRoutine(float duration)
    {
        float timer = 0f;
        int previousRemainTime = -1;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            NotifyRemainTime(SpeedBuffChanged, duration - timer, ref previousRemainTime);
            yield return null;
        }

        SpeedBuffChanged?.Invoke(0f);
        _speedCoroutine = null;
    }

    private void NotifyRemainTime(
        System.Action<float> buffChanged,
        float remainTime,
        ref int previousRemainTime
    )
    {
        int currentRemainTime = Mathf.CeilToInt(Mathf.Max(0f, remainTime));

        if (currentRemainTime == previousRemainTime)
            return;

        previousRemainTime = currentRemainTime;
        buffChanged?.Invoke(currentRemainTime);
    }
}
