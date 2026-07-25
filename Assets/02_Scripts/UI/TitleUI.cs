using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TitleUI : UIBase
{

    [SerializeField] private TMP_Text Text_Title;
    [SerializeField] private Button Button_NewStart;
    [SerializeField] private Button Button_Load;
    [SerializeField] private Button Button_EXIT;



    public void StartNewGame()
    {
        SaveManager.Instance.CreateNewPlayerData();
        GameManager.Instance.StartInGame();
        UIManager.Instance.CloseTitleUI();

    }

    public void LoadGame()
    {
        SaveManager.Instance.LoadPlayerData();
        GameManager.Instance.StartInGame();
        UIManager.Instance.CloseTitleUI();

    }

    public void EXITGame()
    {
        Debug.Log("게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}
