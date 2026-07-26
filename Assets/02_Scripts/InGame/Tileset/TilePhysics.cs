using UnityEngine;

public static class TilePhysics // 방 프리팹의 위치와 회전을 맞추고, 겹침 여부를 체크하는 물리 연산 유틸리티 클래스
{
    public static void AlignRoom(TileController room, TileSocket socketA, TileSocket socketB) // 방 프리팹의 위치와 회전을 맞추는 함수
    {
        if (room == null || socketA == null || socketB == null) return;

        // 스크립트가 붙은 껍데기가 아니라, 실제 위치/회전값을 가진 알맹이를 가져옵니다
        Transform pointA = socketA.ConnectionPoint;
        Transform pointB = socketB.ConnectionPoint;

        // 알맹이(pointA, pointB)의 방향과 위치를 기준으로 연산합니다
        Quaternion targetRotation = Quaternion.LookRotation(-pointA.forward, pointA.up);
        Quaternion differenceRotation = targetRotation * Quaternion.Inverse(pointB.rotation);
        room.transform.rotation = differenceRotation * room.transform.rotation;

        // 위치 맞추기 계산
        Vector3 offset = pointA.position - pointB.position;
        room.transform.position += offset;
    }

    public static bool IsRoomOverlapping(TileController room, LayerMask tileMask) // 방 프리팹이 다른 오브젝트와 겹치는지 체크하는 함수
    {
        if (room == null || room.TileCollider == null) return false;

        room.TileCollider.enabled = false;
        Physics.SyncTransforms();

        Vector3 worldCenter = room.transform.TransformPoint(room.TileCollider.center);

        // [핵심] 스케일 오차 방지 및 '절대 수치(0.5f)'로 박스를 깎아서 문 맞닿음 오차 완전 무시
        Vector3 worldExtents = Vector3.Scale(room.TileCollider.size, room.transform.lossyScale) / 2f;
        float shrinkMargin = 0.5f;

        Vector3 shrunkExtents = new Vector3(
            Mathf.Max(0.1f, worldExtents.x - shrinkMargin),
            Mathf.Max(0.1f, worldExtents.y - shrinkMargin),
            Mathf.Max(0.1f, worldExtents.z - shrinkMargin)
        );

        Collider[] hitColliders = Physics.OverlapBox(worldCenter, shrunkExtents, room.transform.rotation, tileMask);

        room.TileCollider.enabled = true;

        if (hitColliders.Length > 0)
        {
            // Debug.LogWarning($"[물리 겹침 반려] '{room.gameObject.name}' 방이 '{hitColliders[0].gameObject.name}' 오브젝트와 겹쳐서 생성을 취소합니다!"); // 디버그 로그는 너무 많이 찍히므로 주석 처리
            return true;
        }

        return false;
    }
}