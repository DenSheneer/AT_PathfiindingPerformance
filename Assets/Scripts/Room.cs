using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private BuilderSettings _settings;
    [SerializeField] private Material _floorPathMaterial;
    private Floor _floor;
    private Wall[] _walls = new Wall[4];
    private Vector2Int _boardPosition;
    private bool[] _doorBools = { false, false, false, false };
    private List<MyAgent> visited = new List<MyAgent>();


    public int gCost = 0;
    public int hCost = 0;

    public int fCost { get { return  gCost + hCost; } }
    public Room parent = null;

    public Vector2Int BoardPosition { get { return _boardPosition; } }
    public bool[] Doors { get { return _doorBools; } }

    public void Visit(MyAgent agent)
    {
        visited.Add(agent);
        agent.OnDone += agentDispose;
    }
    public bool HasBeenVisitedBy(MyAgent agent)
    {
        return visited.Contains(agent);
    }

    private void agentDispose(MyAgent agent)
    {
        visited.Remove(agent);
        agent.OnDone -= agentDispose;
    }

    public void Initialize(Transform parent, Wall[] walls, Floor floor, Vector2Int boardPosition)
    {
        transform.parent = parent;

        _floor = floor;
        _boardPosition = boardPosition;
        _floor.transform.parent = transform;
        _walls = walls;
        foreach (Wall wall in _walls)
        {
            wall.transform.parent = transform;
        }
    }

    public void UpdateRoom(bool[] newOpenDoors)
    {
        _doorBools = newOpenDoors;

        for (int i = 0; i < _walls.Length; i++)
            if (_doorBools[i]) { _walls[i].MakeDoorInMiddle(); }
    }

    public Vector3 MiddlePosition()
    {
        var middleTile = _floor.GetMiddleTile();
        return middleTile.transform.position;
    }

    public void SetFloorMaterial()
    {
        _floor.SetPathMaterial(_floorPathMaterial);
    }


}
