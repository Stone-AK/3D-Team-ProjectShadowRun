using System.Collections.Generic;

[System.Serializable]
public class PlayerModel
{
    public string PlayerName { get; set; }
    public int Level { get; set; }
    public int CurrentExp { get; set; }
    public int CurrentCredit { get; set; }

    public float CurrentHP { get; set; }
    public float MaxHP { get; set; }
    public float CurrentStamina { get; set; }
    public float MaxStamina { get; set; }

    public ItemModel EquippedHelmet { get; set; }
    public ItemModel EquippedArmor { get; set; }
    public ItemModel EquippedRig { get; set; }
    public ItemModel EquippedBackpack { get; set; }
    public ItemModel SecureContainer { get; set; }

    public ItemModel QuickSlotOne { get; set; }
    public ItemModel QuickSlotTwo { get; set; }
    public ItemModel QuickSlotThree { get; set; }

    public List<ItemModel> InventoryItems { get; set; }

    public List<ItemModel> StashItems { get; set; }

    public List<string> CompletedQuestIds { get; set; }

    public PlayerModel()
    {
        InventoryItems = new List<ItemModel>();
        StashItems = new List<ItemModel>();
        CompletedQuestIds = new List<string>();

        Level = 1;
        CurrentExp = 0;
        MaxHP = 100f;
        CurrentHP = 100f;
        CurrentStamina = 100f;
    }
}