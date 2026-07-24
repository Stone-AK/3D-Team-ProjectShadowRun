using System;
using UnityEngine;

public enum UIRootType
{
    None = 0,
    BackgroundUI,
    MainUI,
    ContentUI,
    PopupUI,
    VeryFrontUI
}

public enum UIType
{
    MainUI,
    Inventory,
    LoadingUI,
    DialogueUI,
    LobbyUI,
    GameBookUI,
    HudUI,
    LocalPlayerProfileUI,
    MVVMTestUI,
    ShopUI,
    ShopItemPopupUI,
    StashUI,
    StartUI,
    PauseUI
}

public static class UIManagerExtension
{
    public static string GetUIPath(this UIManager uiManager, UIRootType uiRootType, UIType uiType)
    {
        string path = string.Empty; // "" == string.Empty

        // 신규UI추가 2) Resources.Load를 할 경로를 직접 명시한다
        // 해당 경로는 프로젝트창에서 Resources/Prefabs/UI폴더 내에 있는 RootType 폴더명과 UIType 프리팹 이름과 동일해야 한다! (ex. ContentUI/DNMyProfilePopup)
        path = $"Prefabs/UI/{uiRootType}/{uiType}";
        return path;
    }

    public static void ShowStartupUIOnGameStart(this UIManager uiManager)
    {
        //uiManager.OpenLoadingUI();
       //uiManager.OpenContentUI(UIType.LobbyUI);
        // uiManager.OpenUI(UIRootType.MainUI, UIType.HudUI);
        //uiManager.OpenUI(UIRootType.MainUI, UIType.MainUI);
        // 게임 로비 UI를 여기서 오픈해주자 -> uiManager.
    }

    public static void OpenInventoryPopup(this UIManager uiManager)
    {
        UIBase uiBase = uiManager.OpenPopupUI(UIType.Inventory);

        if (uiBase == null)
        {
            Debug.LogWarning("Inventory UI가 생성되지 않았습니다.");
            return;
        }

        SetInventoryCursorState(true);
    }

    public static void OpenLoadingUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.VeryFrontUI, UIType.LoadingUI);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseLoadingUI(this UIManager uiManager)
    {
        uiManager.CloseUI(UIRootType.VeryFrontUI, UIType.LoadingUI);
    }

    public static bool IsUIOpened(this UIManager uiManager, UIRootType uiRootType, UIType uiType)
    {
        var uiBase = uiManager.GetOpenedUI(uiRootType, uiType);
        if (uiBase == null)
        {
            return false;
        }
        else
        {
            return uiBase.gameObject.activeSelf;
        }
    }

    public static void CloseInventoryPopup(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.Inventory);
        SetInventoryCursorState(false);
    }

    public static void ToggleInventoryPopup(this UIManager uiManager)
    {
        bool isOpened = uiManager.IsUIOpened(UIType.Inventory);

        if (isOpened)
            uiManager.CloseInventoryPopup();
        else
            uiManager.OpenInventoryPopup();
    }

    public static bool IsInventoryOpened(this UIManager uiManager)
    {
        return uiManager.IsUIOpened(UIType.Inventory);
    }

    private static void SetInventoryCursorState(bool isInventoryOpen)
    {
        Cursor.visible = isInventoryOpen;
        Cursor.lockState = isInventoryOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;
    }

    public static void OpenStartUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.PopupUI, UIType.StartUI);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }
        SetInventoryCursorState(true);
    }

    public static void CloseStartUI(this UIManager uiManager)
    {
        uiManager.CloseUI(UIRootType.PopupUI, UIType.StartUI);
        SetInventoryCursorState(false);
    }

    public static void OpenPauseUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.PopupUI, UIType.PauseUI);
        if (uiBase == null)
        {
            Debug.LogWarning("PauseUI가 생성되지 않았습니다.");
            return;
        }

        Time.timeScale = 0f;
        SetCursorStateForPause(true);
    }

    public static void ClosePauseUI(this UIManager uiManager)
    {
        uiManager.CloseUI(UIRootType.PopupUI, UIType.PauseUI);

        Time.timeScale = 1f;
        SetCursorStateForPause(false);
    }

    public static void TogglePauseUI(this UIManager uiManager)
    {
        bool isOpened = uiManager.IsUIOpened(UIRootType.PopupUI, UIType.PauseUI);

        if (isOpened)
            uiManager.ClosePauseUI();
        else
            uiManager.OpenPauseUI();
    }

    private static void SetCursorStateForPause(bool isPauseOpen)
    {
        Cursor.visible = isPauseOpen;
        Cursor.lockState = isPauseOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

}
