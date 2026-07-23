using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponPartsType
{
    None,       // 없음
    Muzzle,     // 총구
    Scope,      // 조준경
    Magazine,   // 탄창
    Grip,       // 손잡이
    Stock       // 개머리판
}

public enum WeaponStatType
{
    Damage,
    AttackInterval,
    MagazineSize,
    Accuracy,
    Range,
    ReloadTime
}

public enum WeaponStatModifierType
{
    Add,        // 합연산
    Multiply,   // 곱연산
    Override    // 덮어쓰기
}

public struct WeaponStat
{
    public float Damage;
    public float AttackInterval;
    public int MagazineSize;
    public float Accuracy;
    public float Range;
    public float ReloadTime;
}

[System.Serializable]
public class ItemData : BaseData
{
    public string Name;
    public string ItemDescription;
    public string ItemType;
    public string Grade;
    public int MaxStackCount;
    public int SellingPrice;

    public string IconPath;
    public string PrefabPath;

    public string UseItemType;
    public string UseItemParameterList;
    public string[] UseItemParameters;

    public void ParseUseItemParameters()
    {
        if (UseItemParameterList == null || UseItemParameterList == "")
            UseItemParameters = Array.Empty<string>();
        else
            UseItemParameters = UseItemParameterList.Split(',');
    }
}


[System.Serializable]
public class WeaponData : ItemData
{
    public float Damage;
    public int MagazineSize;
    public string AmmoType;
    public float AttackInterval;
    public float Accuracy;
    public float Range;
    public float ReloadTime;
    public float MaxDurability;
}

[System.Serializable]
public class WeaponPartsData : ItemData
{
    public WeaponPartsType PartsType;
    public WeaponStatType StatType;
    public WeaponStatModifierType ModifierType;
    public float Value;
}

[System.Serializable]
public class ItemModel
{
    public string InstanceId;      // 생성될 때마다 발급받는 고유 ID
    public string ItemId;          // DataManager에서 원본 ItemData를 찾기 위한 Key
    public int CurrentStackCount;  // 현재 겹쳐진 개수
}

[System.Serializable]
public class WeaponModel : ItemModel
{
    public int CurrentAmmo;                 // 현재 장전된 총알 수
    public float CurrentDurability;         // 현재 내구도
    public List<ItemModel> AttachedParts;   // 장착된 파츠들
}

public interface InterfaceUseItem
{
    bool TryUseItem( UseableItem itemData );
}

[System.Serializable]
public class UseableItem : ItemData
{
    public float HpPerVariation; // 초당 HP 변화량( Damage, Heal )
    public float ReUseCoolTime; // 재사용 쿨타임
    public float UseDelay; // 사용 대기 시간
    public float Duration; // 사용 지속 시간

}

[System.Serializable]
public class ShopItemData : BaseData 
{
    public string ItemId;        
    public int StockCount;       
}