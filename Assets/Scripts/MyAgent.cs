using NUnit.Framework;
using System.Collections.Generic;
using MyBox;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using System;
using System.IO;
using UnityEditor.Experimental.GraphView;

public class MyAgent : MonoBehaviour
{
    [SerializeField] DungeonGenerator _dungeon;
    [SerializeField] Room _target = null;

    private Room _start;

    public UnityAction<MyAgent> OnDone;

    public void Start()
    {
        _dungeon.OnDungeonGenerated += (deepestRoom) =>
        {
            _target = deepestRoom;
            _start = _dungeon.RoomOnBoard[new Vector2Int(0, 0)];
            transform.position = _start.MiddlePosition();
        };
    }
    [ButtonMethod]
    public void TestNoAsync()
    {
        var timeNow = DateTime.Now;
        var path = PathfindTo(_target);
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("No Async duration in milliseconds: " + duration.Milliseconds);

        drawPath(path);
    }

    [ButtonMethod]
    public async void TestAsync()
    {
        var timeNow = DateTime.Now;
        var path = await PathfindToAsync(_target);
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("Async duration in milliseconds: " + duration.Milliseconds);

        drawPath(path);
    }

    [ButtonMethod]
    public async void TestMultithread()
    {
        var timeNow = DateTime.Now;
        var path = await Task.Run(() => PathfindToMultithread(_target));
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("Multithread duration in milliseconds: " + duration.Milliseconds);

        drawPath(path);
    }

    [ButtonMethod]
    public void TestAStar()
    {
        var path = PathfindAStar();
        drawPath(path);
    }

    private void drawPath(List<Room> path)
    {
        if (path != null)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                path[i].SetFloorMaterial();
                Debug.DrawLine(path[i].MiddlePosition(), path[i + 1].MiddlePosition(), UnityEngine.Color.green, 60.0f);
            }
        }
    }

    public List<Room> PathfindTo(Room target)
    {
        if (target == null) { return null; }
        List<Cell> board = new List<Cell>();
        for (int i = 0; i < _dungeon.Board.Count; i++)
            board.Add(new Cell());

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        VisitNode(_start, target, path, solution, 0);

        OnDone?.Invoke(this);
        Debug.Log($"Found solution with {solution.Count} steps");

        return solution;
    }

    public async Task<List<Room>> PathfindToMultithread(Room target)
    {
        if (target == null) { return null; }
        List<Cell> board = new List<Cell>();
        for (int i = 0; i < _dungeon.Board.Count; i++)
            board.Add(new Cell());

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        await Task.Run(() => VisitNodeMultithread(_start, target, path, solution, 0));

        OnDone?.Invoke(this);
        Debug.Log($"Found solution with {solution.Count} steps");

        return solution;
    }

    public async Task<List<Room>> PathfindToAsync(Room target)
    {
        if (target == null) { return null; }
        List<Cell> board = new List<Cell>();
        for (int i = 0; i < _dungeon.Board.Count; i++)
            board.Add(new Cell());

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        await VisitNodeAsync(_start, target, path, solution, 0);

        OnDone?.Invoke(this);
        Debug.Log($"Found solution with {solution.Count} steps");

        return solution;
    }


    public bool VisitNode(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs)
    {
        if (currentRoom.HasBeenVisitedBy(this))
            return false;

        currentRoom.Visit(this);

        if (currentRoom == target)
        {
            solution.Add(currentRoom);
            return true;
        }

        var neighbours = _dungeon.GetRoomNeighbours(this, currentRoom);

        foreach (var neighbor in neighbours)
        {
            if (VisitNode(neighbor, target, path, solution, runs))
            {
                solution.Add(currentRoom);
                return true;
            }
        }

        return false;
    }

    public async Task<bool> VisitNodeAsync(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs)
    {
        if (currentRoom.HasBeenVisitedBy(this))
            return false;

        currentRoom.Visit(this);

        if (currentRoom == target)
        {
            solution.Add(currentRoom);
            return true;
        }

        var neighbours = _dungeon.GetRoomNeighbours(this, currentRoom);

        foreach (var neighbor in neighbours)
        {
            if (await VisitNodeAsync(neighbor, target, path, solution, runs))
            {
                solution.Add(currentRoom);
                return true;
            }
        }

        return false;
    }

    public async Task<bool> VisitNodeMultithread(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs)
    {
        if (currentRoom.HasBeenVisitedBy(this))
            return false;

        currentRoom.Visit(this);

        if (currentRoom == target)
        {
            solution.Add(currentRoom);
            return true;
        }

        var neighbours = _dungeon.GetRoomNeighbours(this, currentRoom);

        foreach (var neighbor in neighbours)
        {
            if (await Task.Run(() => VisitNodeMultithread(neighbor, target, path, solution, runs)))
            {
                solution.Add(currentRoom);
                return true;
            }
        }

        return false;
    }

    public List<Room> PathfindAStar()
    {
        if (_target == null) { return null; }
        {
            List<Room> openSet = new List<Room>();
            HashSet<Room> closedSet = new HashSet<Room>();

            openSet.Add(_start);

            while (openSet.Count > 0)
            {
                Room currentRoom = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentRoom.fCost || openSet[i].fCost == currentRoom.fCost && openSet[i].hCost < currentRoom.hCost)
                    {
                        currentRoom = openSet[i];
                    }
                }
                openSet.Remove(currentRoom);
                closedSet.Add(currentRoom);
                if (currentRoom == _target)
                {
                    return retracePath(_start, _target);
                }

                foreach (var neighbour in _dungeon.GetRoomNeighbours(currentRoom))
                {
                    if (closedSet.Contains(neighbour)) { continue; }

                    int newMoveCostToNeighbour = currentRoom.gCost + getDistance(currentRoom, neighbour);
                    if (newMoveCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMoveCostToNeighbour;
                        neighbour.hCost = getDistance(neighbour, _target);
                        neighbour.parent = currentRoom;

                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                    }
                }
            }
        }
        return null;
    }

    private List<Room> retracePath(Room start, Room end)
    {
        List<Room> path = new List<Room>();
        Room currentRoom = end;

        while (currentRoom != start)
        {
            path.Add(currentRoom);
            currentRoom.SetFloorMaterial();

            currentRoom = currentRoom.parent;
        }
        path.Reverse();
        return path;
    }

    private int getDistance(Room roomA, Room roomB)
    {
        int distX = Mathf.Abs(roomA.BoardPosition.x - roomB.BoardPosition.x);
        int distY = Mathf.Abs(roomA.BoardPosition.y - roomB.BoardPosition.y);

        if (distX > distY)
        {
            return 14 * distY + 10 * (distX - distY);
        }
        return 14 * distX + 10 * (distY - distX);
    }


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestNoAsync();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestAsync();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestMultithread();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestAStar();
        }
    }
}
