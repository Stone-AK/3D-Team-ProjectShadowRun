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
    TitleUI,
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

    public static void OpenTitleUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.VeryFrontUI, UIType.TitleUI);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }
        SetInventoryCursorState(true);
    }

    public static void CloseTitleUI(this UIManager uiManager)
    {
        uiManager.CloseUI(UIRootType.VeryFrontUI, UIType.TitleUI);
        SetInventoryCursorState(false);
    }

    public static void TogglePauseUI(this UIManager uiManager, PlayerInputHandler inputHandler = null)
    {
        if (uiManager.IsUIOpened(UIType.PauseUI))
        {
            uiManager.ClosePauseUI(inputHandler);
        }
        else
        {
            uiManager.OpenPauseUI(inputHandler);
        }
    }

    public static void OpenPauseUI(this UIManager uiManager, PlayerInputHandler inputHandler = null)
    {
        uiManager.OpenPopupUI(UIType.PauseUI);

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (inputHandler != null)
        {
            inputHandler.SetGameplayInputBlocked(true);
        }
    }

    public static void ClosePauseUI(this UIManager uiManager, PlayerInputHandler inputHandler = null)
    {
        uiManager.ClosePopupUI(UIType.PauseUI);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (inputHandler == null)
        {
            inputHandler = UnityEngine.Object.FindFirstObjectByType<PlayerInputHandler>();
        }

        if (inputHandler != null)
        {
            inputHandler.SetGameplayInputBlocked(false);
        }
    }

}
