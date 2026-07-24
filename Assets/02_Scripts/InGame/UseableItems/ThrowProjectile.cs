using UnityEngine;

public class ThrowProjectile : MonoBehaviour, IQuickSlotConsumeHandler
{
    [SerializeField] private Transform _throwPoint;      // 투척 시작 위치 (카메라 앞/플레이어 손)
    [SerializeField] private float _defaultThrowForce = 15f; // 기본 투척력

    public bool CanHandleType( ItemData itemData )
    {
        if (itemData == null)
        {
            return false;
        }

        // ItemType이 Throwable이면서 UseItemType이 맞는 경우만 처리
        bool isThrowable = itemData.ItemType == "Throwable";
        bool isValidUseType = itemData.UseItemType == "Explosion" ||
                             itemData.UseItemType == "EMP" ||
                             itemData.UseItemType == "Smoke";

        return isThrowable && isValidUseType;
    }

    public void UseItem( ItemData itemData )
    {
        OnThrowConsumable(itemData);
    }
    public void OnThrowConsumable( ItemData itemData )
    {
        if (itemData == null || string.IsNullOrWhiteSpace(itemData.PrefabPath))
        {
            return;
        }

        // 투척 위치 계산
        Vector3 spawnPosition = transform.position;
        if (_throwPoint != null)
        {
            spawnPosition = _throwPoint.position;
        }

        // 오브젝트 풀 매니저로 수류탄 프리팹 가져오기  
        GameObject grenadeObject = ObjectPoolManager.Instance.GetFromPool(itemData.PrefabPath);

        if (grenadeObject == null)
        {
            Debug.LogWarning("풀에서 프리팹을 가져올 수 없습니다: " + itemData.PrefabPath);
            return;
        }

        grenadeObject.transform.position = spawnPosition;
        grenadeObject.transform.rotation = Quaternion.identity;

        // 수류탄 생성 시 파라미터 데이터 및 투척 방향 전달
        ActivateGrenade activeGrenade = grenadeObject.GetComponent<ActivateGrenade>();
        if (activeGrenade != null)
        {
            Vector3 throwDirection = transform.forward;
            activeGrenade.InitGrenade(itemData, throwDirection, _defaultThrowForce);
        }

    }
}