using System.Collections;
using UnityEngine;

public class InGameExit : MonoBehaviour
{
    [Header("탈출 설정")]
    [Tooltip("트리거 진입 후 로비로 귀환하기까지 대기해야 하는 시간 (초)")]
    [SerializeField] private float _waitTime = 3.0f;

    [Tooltip("트리거에 반응할 대상의 태그 (예: Player)")]
    [SerializeField] private string _targetTag = "Player";

    private Coroutine _exitCoroutine;

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
            if (_exitCoroutine != null)
            {
                StopCoroutine(_exitCoroutine);
            }

            _exitCoroutine = StartCoroutine(ExitRoutine(other.gameObject));
            Debug.Log($"[{other.name}] 탈출 구역 진입! {_waitTime}초 귀환 타이머를 시작합니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_targetTag))
        {
            if (_exitCoroutine != null)
            {
                StopCoroutine(_exitCoroutine);
                _exitCoroutine = null;
                Debug.Log($"[{other.name}] 탈출 구역을 이탈하여 귀환 타이머가 취소되었습니다.");
            }
        }
    }

    private IEnumerator ExitRoutine(GameObject targetObject)
    {
        yield return new WaitForSeconds(_waitTime);

        Debug.Log($"[{targetObject.name}] 탈출 성공! 로비(OutGame) 작업 공간으로 전환합니다.");

        if (GameManager.Instance != null)
        {
            PlayerStatus.Instance?.RestoreStatus();
            GameManager.Instance.ReturnToOutGame();
        }
        else
        {
            Debug.LogError("InGameExit: GameManager 싱글톤 인스턴스를 찾을 수 없습니다!");
        }
    }
}
