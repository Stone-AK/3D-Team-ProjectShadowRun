using UnityEngine;

public interface IQuickSlotConsumeHandler
{
    // 처리 가능한 아이템 타입인지 확인
    bool CanHandleType( ItemData itemData );

    // 아이템 효과 실행
    void UseItem( ItemData itemData );
}

public class PlayerQuickSlotHandler : MonoBehaviour
{
    private IQuickSlotConsumeHandler[] _handlers;

    private void Awake( )
    {
        // 내 오브젝트나 자식에 붙어있는 모든 IQuickSlotConsumeHandler 수집
        _handlers = GetComponentsInChildren<IQuickSlotConsumeHandler>();
    }

    private void Start( )
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnConsumableUsed -= HandleItemUsed;
            InventoryManager.Instance.OnConsumableUsed += HandleItemUsed;
        }
    }

    private void OnDisable( )
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnConsumableUsed -= HandleItemUsed;
        }
    }

    private void HandleItemUsed( ItemData itemData )
    {
        if (itemData == null || string.IsNullOrWhiteSpace(itemData.UseItemType))
        {
            return;
        }

        // 해당 itemData들을 처리할 수 있는 핸들러를 찾아 실행
        for (int i = 0; i < _handlers.Length; i++)
        {
            if (_handlers[i].CanHandleType(itemData))
            {
                _handlers[i].UseItem(itemData);
                break;
            }
        }
    }
}