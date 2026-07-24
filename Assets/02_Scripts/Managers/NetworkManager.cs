using System.Collections;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Inst { get; set; }

    public NetworkShopService ShopService { get; private set; }
    public NetworkStashService StashService { get; private set; }


    private void Awake()
    {
        Inst = this;
        InitNetworkService();
    }

    private IEnumerator Start() // PlayerStatus 의 생성시기를 일찍 끌어올 것.
    {
        yield return null;

        LoadGameData();
    }

    private void InitNetworkService()
    {
        ShopService = new NetworkShopService();
        StashService = new NetworkStashService();
    }

    private void LoadGameData()
    {
        PlayerModel playerData = SaveManager.Instance.LoadPlayerData();

        var stashVm = StashService.GetStashViewModel();
        stashVm.CurPlayerCredit = playerData.CurrentCredit;

        ShopService.GetShopViewModel().CurPlayerCredit = playerData.CurrentCredit;
        stashVm.CurPlayerCredit = playerData.CurrentCredit;
    }
}
