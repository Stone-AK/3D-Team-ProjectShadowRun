using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ShopItemPopupUI : UIBase //생각해보니까 이거 그냥 인벤토리나 창고에서 돌려써도 괜찮을것 같음.
{
    [SerializeField] private Image Image_ItemIcon;
    [SerializeField] private TMP_Text Text_ItemName;
    [SerializeField] private TMP_Text Text_ItemDescription;
    [SerializeField] private TMP_Text Text_ItemSellingPrice; 

    private RectTransform _popupRectTransform;

    private void Awake() 
    {
        _popupRectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        UpdatePopupPosition();
    }

    public void SetItemData(string itemDataId)
    {
        if (itemDataId == null)
        {
            return;
        }
        else
        {
            var itemData = DataManager.Instance.GetItemData(itemDataId);

            if (itemData != null)
            {
                Image_ItemIcon.sprite = ItemIconLoader.LoadIcon(DataManager.Instance.GetItemData(itemDataId));
                Text_ItemName.text = itemData.Name;
                Text_ItemDescription.text = itemData.ItemDescription;
                Text_ItemSellingPrice.text = $"{itemData.SellingPrice} Credit";
            }
            
            // 화면 최상단으로 끌어올리기 (다른 UI에 가려짐 방지)
            _popupRectTransform.SetAsLastSibling();

            // 켜지는 즉시 마우스 위치로 순간이동 (안 그러면 1프레임 동안 중앙에 보임)
            UpdatePopupPosition();
        }
    }

    private void UpdatePopupPosition()
    {
        if (Mouse.current == null || _popupRectTransform == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 offset = new Vector2(10f, -10f);
        Vector2 targetPos = mousePos + offset;

        float popupWidth = _popupRectTransform.rect.width;
        float popupHeight = _popupRectTransform.rect.height;

        float minY = popupHeight * _popupRectTransform.pivot.y;
        float maxY = Screen.height - (popupHeight * (1f - _popupRectTransform.pivot.y));
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        float minX = popupWidth * _popupRectTransform.pivot.x;
        float maxX = Screen.width - (popupWidth * (1f - _popupRectTransform.pivot.x));
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);

        _popupRectTransform.position = targetPos;
    }
}
