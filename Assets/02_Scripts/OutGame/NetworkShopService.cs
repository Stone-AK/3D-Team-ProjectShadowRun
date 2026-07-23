using System.Collections.Generic;
using UnityEngine;

public class NetworkShopService
{
    private ShopViewModel _shopViewModel;

    public ShopViewModel GetShopViewModel()
    {
        if (_shopViewModel == null)
        {
            CreateShopViewModel();
        }

        return _shopViewModel;
    }

    private ShopViewModel CreateShopViewModel()
    {
        var shopVm = new ShopViewModel();
        _shopViewModel = shopVm;
        return shopVm;
    }

    public void InitShopData()
    {
        var vm = GetShopViewModel();
        int slotIndex = 0;

        // 1. 기존 슬롯 초기화 (빈 슬롯으로 세팅)
        foreach (var slot in vm.ShopItemSlotList)
        {
            slot.IsSlotEmpty = true;
        }

        // 2. 고정으로 보여줄 기초 힐템 ID 목록 세팅 
        // (실제 기획 데이터에 있는 힐템 ID로 변경해주세요. 예: "Consumable_Bandage")
        List<string> fixedItemIds = new List<string> { "Item_Medical_Bandage", "Item_Food_Water" };

        foreach (var fixedId in fixedItemIds)
        {
            if (slotIndex >= vm.ShopItemSlotList.Count) break;

            // 고정 아이템 추가 (재고는 5개로 임의 설정)
            SetShopItemSlot(vm.ShopItemSlotList[slotIndex], fixedId, 5);
            slotIndex++;
        }

        // 3. 랜덤으로 등장할 후보군 ID 리스트 추출
        List<string> randomCandidateIds = new List<string>();

        // DataManager에 있는 실제 전체 아이템 딕셔너리 순회
        foreach (var item in DataManager.Instance._itemDataDic.Values)
        {
            // 이미 고정으로 들어간 아이템은 후보군에서 제외
            // BaseData를 상속받았으므로 ID 접근이 item.Id 또는 item.ItemId 일 수 있습니다.
            if (fixedItemIds.Contains(item.Id)) continue;

            // [핵심 필터링] ItemData.cs의 string ItemType을 기준으로 비교
            // 기획 엑셀에 적으신 무기(Weapon), 소모품(Consumable) 등의 정확한 텍스트를 적어주세요.
            if (item.ItemType == "Weapon" || item.ItemType == "Consumable")
            {
                // 만약 Consumable 중에 잡템이 섞여있다면 아래처럼 UseItemType 등을 추가로 검사할 수 있습니다.
                // if (item.ItemType == "Consumable" && item.UseItemType != "Heal") continue;

                randomCandidateIds.Add(item.Id);
            }
        }

        // 4. 추출된 후보군 리스트 무작위 섞기 (셔플)
        ShuffleList(randomCandidateIds);

        // 5. 남은 상점 슬롯에 랜덤 아이템 채워넣기
        int randomItemLimit = 5; // 상점에 띄울 랜덤 아이템의 최대 개수
        int addedRandomCount = 0;

        foreach (var randomId in randomCandidateIds)
        {
            // 상점 슬롯이 꽉 찼거나, 지정한 랜덤 개수를 다 채웠으면 종료
            if (slotIndex >= vm.ShopItemSlotList.Count || addedRandomCount >= randomItemLimit) break;

            // 랜덤 아이템 추가 (재고는 1개로 세팅)
            SetShopItemSlot(vm.ShopItemSlotList[slotIndex], randomId, 1);
            slotIndex++;
            addedRandomCount++;
        }

        // 6. 플레이어 재화 및 인벤토리/창고 연동 (기존 로직 유지)
        PlayerModel playerData = SaveManager.Instance.LoadPlayerData();
        vm.CurPlayerCredit = playerData.CurrentCredit;

        var inventoryItems = InventoryManager.Instance.ItemList;
        LoadPlayerItemsToShopZone(new List<ItemModel>(inventoryItems), vm.InventoryItemSlotList);
        LoadPlayerItemsToShopZone(playerData.StashItems, vm.StashItemSlotList);
    }

    // 리스트를 무작위로 섞어주는 유틸리티 메서드
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    // 이하 상점UI내에서 일어난 데이터 변동을 UI가 닫히며 저장하고 동기화 시키는 메서드. 인벤토리에 데이터를 덮어씌우는 메서드(SyncInventoryFromUI) 추가 요청할 것.
    public void SyncDataOnClose()
    {
        var vm = GetShopViewModel();
        PlayerModel playerData = SaveManager.Instance.LoadPlayerData();

        // 1. 변동된 크레딧 갱신
        playerData.CurrentCredit = vm.CurPlayerCredit;

        List<ItemModel> newStash = new List<ItemModel>();
        foreach (var slot in vm.StashItemSlotList)
        {
            if (!slot.IsSlotEmpty)
            {
                newStash.Add(new ItemModel
                {
                    InstanceId = slot.ItemUniqueId,
                    ItemId = slot.ItemDataId,
                    CurrentStackCount = slot.ItemStackCount
                });
            }
        }

        playerData.StashItems = newStash;
        SaveManager.Instance.SavePlayerData(playerData);
    }

    private void SetShopItemSlot(ShopItemSlotViewModel slot, string dataId, int count)
    {
        var itemData = DataManager.Instance.GetItemData(dataId);

        slot.ItemUniqueId = string.Empty;
        slot.ItemDataId = itemData.Id;
        slot.ItemStackCount = count;
        slot.ItemSellingPrice = itemData.SellingPrice; // 구매가
        slot.IsSlotEmpty = false;
    }

    private void LoadPlayerItemsToShopZone(List<ItemModel> savedItems, List<ShopItemSlotViewModel> targetSlots)
    {
        foreach (var slot in targetSlots) slot.IsSlotEmpty = true;

        for (int i = 0; i < savedItems.Count; i++)
        {
            if (i >= targetSlots.Count) break;

            var savedItem = savedItems[i];
            var itemData = DataManager.Instance.GetItemData(savedItem.ItemId);

            targetSlots[i].ItemUniqueId = savedItem.InstanceId; // 유저 아이템은 유니크 ID 유지
            targetSlots[i].ItemDataId = savedItem.ItemId;
            targetSlots[i].ItemStackCount = savedItem.CurrentStackCount;
            targetSlots[i].ItemSellingPrice = itemData != null ? itemData.SellingPrice : 0; // 판매가
            targetSlots[i].IsSlotEmpty = false;
        }
    }

    //플레이어가 상점에서 아이템을 구매할 때 사용되는 함수
    public void RequestBuyItem(int shopSlotIndex)
    {
        var vm = GetShopViewModel();
        var targetSlot = vm.ShopItemSlotList[shopSlotIndex];

        if (targetSlot.IsSlotEmpty) return;

        int price = targetSlot.ItemSellingPrice;

        if (vm.CurPlayerCredit < price)
        {
            Debug.LogError("크레딧이 부족합니다.");
            return;
        }

        // 2. 인벤토리나 창고에 빈 공간이 있는지 찾기. 인벤토리, 창고 구현 후 주석 해제
        //ShopItemSlotViewModel emptySlot = vm.InventorySlots.Find(s => s.IsEmpty);
        //if (emptySlot == null)
        //{
        //    emptySlot = vm.StashItemSlotList.Find(s => s.IsSlotEmpty);
        //}

        //if (emptySlot == null)
        //{
        //    Debug.LogError("아이템을 넣을 공간이 없습니다.");
        //    return;
        //}

        // 3. 구매 처리
        //vm.CurPlayerCredit -= price;
        //long generatedUniqueId = System.DateTime.Now.Ticks; // 임시 유니크 ID 생성

        //emptySlot.SetItem(generatedUniqueId, targetSlot.ItemData, 1);

        Debug.Log($"구매 성공!");

    }

    //플레이어가 상점에 아이템을 판매할 때 사용되는 함수
    public void RequestSellItem(ShopItemSlotType fromZone, int slotIndex)
    {
        //var vm = GetShopViewModel();
        //List<ShopItemSlotViewModel> targetZone = (fromZone == ShopItemSlotType.Inventory) ? vm.InventorySlots : vm.StashSlots;

        //var slot = targetZone[slotIndex];
        //if (slot.IsSlotEmpty) return;

        //// 가격 지급 및 슬롯 초기화
        //vm.CurPlayerCredit += slot.ItemData.SellingPrice;
        //slot.Clear();

        Debug.Log("아이템 판매 성공!");
    }
}
