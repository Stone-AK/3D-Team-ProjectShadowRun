using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
public class CoverWallInfo
{
    public CoverWall Wall;
    public CoverHidePoint SelectedHidePoint;
    public Transform PeekPoint;
    public Vector3 HidePosition;   // 실제 이동 좌표
    public Vector3 PeekPosition;
}
[System.Serializable]
public class CoverHidePoint
{
    public Transform HidePoint;
    public Transform PeekLeft;
    public Transform PeekRight;
}
public class CoverWall : MonoBehaviour
{
    private List<EnemyBase> _wallUsers = new List<EnemyBase>();
    [SerializeField] private int _userCount = 3;
    [SerializeField] private float _userOffset = 1f;
    //[SerializeField] private float _userSpacing = 0.4f;
    [SerializeField] private float _peekForwardOffset = 1f;
    private BattleAgentTeamType _currentTeamType = BattleAgentTeamType.None;
    [SerializeField] public CoverHidePoint _coverHidePoint1;
    [SerializeField] public CoverHidePoint _coverHidePoint2;
    public bool CanRegisterUserToWall(EnemyBase newUser)
    {
        if (_wallUsers.Contains(newUser))
        {
            return true;
        }
        if (!CanUseWall(newUser))
        {
            return false;
        }

        return true;
    }
    public void RegisterUserToWall(EnemyBase newUser) 
    {
        if (_wallUsers.Contains(newUser))
        {
            return ;
        }
        _wallUsers.Add(newUser);

        if (_currentTeamType == BattleAgentTeamType.None)
        {
            _currentTeamType = newUser.Team;
        }
    }
    public void ReleaseWallUser(EnemyBase newUser)
    {
        if (!_wallUsers.Contains(newUser))
        {
            return;
        }
        _wallUsers.Remove(newUser);
        if (_wallUsers.Count == 0)
        {
            _currentTeamType = BattleAgentTeamType.None;
        }
    }
    public bool CanUseWall(EnemyBase newUser)
    {
        if (_currentTeamType == BattleAgentTeamType.None && _wallUsers.Count == 0)
        {
            return true;
        }
        else if (_currentTeamType == newUser.Team && _wallUsers.Count < _userCount)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private float GetOffsetByIndex(int index)
    {
        if (index == 0)
            return 0f;

        int step = (index + 1) / 2;

        return (index % 2 == 1) ? step * _userOffset : -step * _userOffset;
    }
    public Vector3 ReturnHidePoint(EnemyBase user, Transform hidePoint)
    {
        int index = _wallUsers.IndexOf(user);

        if (index < 0)
        {
            return hidePoint.position;
        }

        float offset = GetOffsetByIndex(index);

        return hidePoint.position + transform.right * offset;
    }
    public Vector3 ReturnPeekPoint(EnemyBase user, Transform peekPoint)
    {
        int index = _wallUsers.IndexOf(user);

        if (index < 0)
        {
            return peekPoint.position;
        }

        float offset = GetOffsetByIndex(index);

        return peekPoint.position + transform.forward * offset + transform.right * offset;
    }
}
