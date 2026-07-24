using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : UIBase
{
    [Header("Buttons")]
    [SerializeField] private Button Button_EmergencyEscape;
    [SerializeField] private Button Button_Save;
    [SerializeField] private Button Button_Resume;
    [SerializeField] private Button Button_QuitGame;

    private void Start()
    {
        if (Button_EmergencyEscape != null)
            Button_EmergencyEscape.onClick.AddListener(OnClickEmergencyEscape);

        if (Button_Save != null)
            Button_Save.onClick.AddListener(OnClickSave);

        if (Button_Resume != null)
            Button_Resume.onClick.AddListener(OnClickResume);

        if (Button_QuitGame != null)
            Button_QuitGame.onClick.AddListener(OnClickQuitGame);
    }

    private void OnClickEmergencyEscape()
    {
        Debug.Log("긴급 탈출: 인벤토리 아이템을 잃고 로비로 돌아갑니다.");

        Time.timeScale = 1f;

        // 인벤토리 비우는 로직 생성 예정

        UIManager.Instance.ClosePauseUI();
        GameManager.Instance.ReturnToOutGame();

    }

    private void OnClickSave()
    {
        Debug.Log("게임 데이터를 저장합니다.");
        SaveManager.Instance.SavePlayerData(PlayerStatus.Instance.Model);
    }

    private void OnClickResume()
    {
        Debug.Log("게임 재개");
        UIManager.Instance.ClosePauseUI();
    }

    private void OnClickQuitGame()
    {
        Debug.Log("게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
