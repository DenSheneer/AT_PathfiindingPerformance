using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private BuilderSettings _settings;
    private Floor _floor;
    private Wall[] _walls = new Wall[4];
    private Vector2Int _boardPosition;
    private bool[] _doorBools = { false, false, false, false };
    private HashSet<PathfindAgent> visited = new HashSet<PathfindAgent>();

    public Vector2Int BoardPosition { get { return _boardPosition; } }
    public bool[] Doors { get { return _doorBools; } set { _doorBools = value; } }

    public RoomData RoomData;
    public Dictionary<PathfindAgent, RoomData> pathfindingData = new Dictionary<PathfindAgent, RoomData>();

    public void Visit(PathfindAgent agent)
    {
        visited.Add(agent);
        agent.OnDFS_Done += agentDispose;
    }
    public bool HasBeenVisitedBy(PathfindAgent agent)
    {
        return visited.Contains(agent);
    }

    private void agentDispose(PathfindAgent agent)
    {
        visited.Remove(agent);
        pathfindingData.Remove(agent);

        agent.OnDFS_Done -= agentDispose;
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

    public void SetFloorMaterial(Material material)
    {
        _floor.SetPathMaterial(material);
    }
    public void RestoreDefaultFloor()
    {
        _floor.RestoreDefaultFloor();
    }

    public void MakeNewRoomData(PathfindAgent agent, RoomData roomData)
    {
        pathfindingData.Add(agent, roomData);
        agent.OnDFS_Done += agentDispose;
    }
}

public class RoomData : IHeapItem<RoomData>
{
    public int gCost = 0;
    public int hCost = 0;
    private int heapIndex;
    public Room roomObject;

    public RoomData(Room room)
    {
        roomObject = room;
    }

    public int fCost { get { return gCost + hCost; } }
    public RoomData parent = null;
    public int HeapIndex { get => heapIndex; set => heapIndex = value; }

    public int CompareTo(RoomData other)
    {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(other.hCost);

        }
        return -compare;
    }
}
