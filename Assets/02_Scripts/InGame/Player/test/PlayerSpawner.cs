using System.Runtime.Serialization;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject PlayerPrefab;

    private void Start()
    {
        GameObject existingPlayer = GameObject.FindWithTag("Player");

        if (existingPlayer != null)
        {
            Debug.Log("PlayerSpawner: 기존 플레이어를 발견했습니다! 위치를 스폰 포인트로 이동시킵니다.");
            MoveExistingPlayer(existingPlayer);

            BindPlayerCamera(existingPlayer);
            BindPlayerHUD(existingPlayer);
        }
        else
        {
            Debug.Log("PlayerSpawner: 플레이어가 존재하지 않습니다. 새로 생성합니다.");
            SpawnNewPlayer();
        }
    }

    private void MoveExistingPlayer(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.transform.position = transform.position;
        player.transform.rotation = transform.rotation;

        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    private void SpawnNewPlayer()
    {
        GameObject player = GameObjectManager.Instance.SpawnObject(
            PlayerPrefab,
            transform.position,
            transform.rotation
        );

        if (player == null)
        {
            Debug.LogError("PlayerPrefab이 할당되지 않았거나 생성에 실패했습니다.");
            return;
        }

        BindPlayerCamera(player);
        BindPlayerHUD(player);
    }

    private void BindPlayerCamera(GameObject player)
    {
        PlayerSight playerSight = player.GetComponent<PlayerSight>();

        if (playerSight == null)
        {
            Debug.LogError("생성된 Player에 PlayerSight가 없습니다.");
            return;
        }

        Transform cameraTarget = playerSight.GetPlayerSightTransform();

        if (GameManager.Instance != null && GameManager.Instance.PlayerCinemachineCamera != null)
        {
            GameManager.Instance.PlayerCinemachineCamera.Bind(cameraTarget);
            Debug.Log("PlayerSpawner: 메인 카메라 바인딩 완료!");
        }
        else
        {
            Debug.LogError("PlayerSpawner: GameManager에 카메라 바인더가 세팅되지 않았습니다!");
        }
    }

    private void BindPlayerHUD(GameObject player)
    {
        PlayerStatus playerStatus = player.GetComponent<PlayerStatus>();
        PlayerItemInteractor itemInteractor = player.GetComponent<PlayerItemInteractor>();
        PlayerWeaponController weaponController = player.GetComponent<PlayerWeaponController>();

        if (playerStatus == null)
        {
            Debug.LogError("생성된 Player에 PlayerStatus가 없습니다.");
            return;
        }

        if (itemInteractor == null)
        {
            Debug.LogError("생성된 Player에 PlayerItemInteractor가 없습니다.");
            return;
        }

        if (weaponController == null)
        {
            Debug.LogError("생성된 Player에 PlayerWeaponController가 없습니다.");
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager 인스턴스가 존재하지 않습니다.");
            return;
        }

        UIBase uiBase = UIManager.Instance.OpenUI(UIRootType.MainUI, UIType.HudUI);
        PlayerHUDView hudView = uiBase as PlayerHUDView;

        if (hudView == null)
        {
            Debug.LogError("HudUI 루트에 PlayerHUDView가 없습니다.");
            return;
        }

        hudView.BindViewModel(playerStatus.ViewModel);
        hudView.BindItemInfoUI(itemInteractor);
        hudView.BindWeaponController(weaponController);
    }
}
