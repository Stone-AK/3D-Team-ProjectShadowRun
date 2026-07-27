using System.Collections;
using UnityEngine;

public class LobbyDoor : MonoBehaviour
{
    [Header("이동 설정")]

    [Tooltip("트리거 진입 후 대기해야 하는 시간 (초)")]
    [SerializeField] private float _waitTime = 3.0f;

    [Tooltip("트리거에 반응할 대상의 태그 (예: Player)")]
    [SerializeField] private string _targetTag = "Player";

    private Coroutine _teleportCoroutine;

    private void Awake()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_targetTag))
        {
            if (_teleportCoroutine != null)
            {
                StopCoroutine(_teleportCoroutine);
            }

            Lobby.Instance.ClearInteractableTarget();
            _teleportCoroutine = StartCoroutine(TeleportRoutine(other.gameObject));
            Debug.Log($"[{other.name}] 트리거 진입! {_waitTime}초 대기 타이머를 시작합니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_targetTag))
        {
            if (_teleportCoroutine != null)
            {
                StopCoroutine(_teleportCoroutine);
                _teleportCoroutine = null;
                Debug.Log($"[{other.name}] 범위를 이탈하여 타이머가 초기화되었습니다.");
            }
        }
    }

    private IEnumerator TeleportRoutine(GameObject targetObject)
    {
        yield return new WaitForSeconds(_waitTime);

        Debug.Log($"[{targetObject.name}] InGame 공간으로 전환합니다.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartInGame();
        }
        else
        {
            Debug.LogError("StartInGame: GameManager 싱글톤 인스턴스를 찾을 수 없습니다!");
        }
    }
}
