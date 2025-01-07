using NUnit.Framework;
using System.Collections.Generic;
using MyBox;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;
using System.IO;
using System;
using UnityEngine.Profiling;
using UnityEngine.InputSystem;
using System.Drawing;

public class MyAgent : MonoBehaviour
{
    [SerializeField] DungeonGenerator _dungeon;
    [SerializeField] Room _target = null;

    private Room _start;

    public UnityAction<MyAgent> OnDone;

    public void Start()
    {
        if (_dungeon != null)
        {
            _start = _dungeon.RoomOnBoard[new Vector2Int(0, 0)];
            transform.position = _start.MiddlePosition();

        }
    }
    [ButtonMethod]
    public void TestNoAsync()
    {
        var timeNow = DateTime.Now;
        PathfindTo(_target);
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("No Async duration in milliseconds: " + duration.Milliseconds);
    }

    [ButtonMethod]
    public async void TestAsync()
    {
        var timeNow = DateTime.Now;
        var path = await PathfindToAsync(_target);
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("Async duration in milliseconds: " + duration.Milliseconds);


        if (path != null)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i].MiddlePosition(), path[i + 1].MiddlePosition(), UnityEngine.Color.green, 60.0f);
            }
        }
    }

    [ButtonMethod]
    public async void TestMultithread()
    {
        var timeNow = DateTime.Now;
        var path = await Task.Run(() => PathfindToMultithread(_target));
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("Multithread duration in milliseconds: " + duration.Milliseconds);

        if (path != null)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i].MiddlePosition(), path[i + 1].MiddlePosition(), UnityEngine.Color.green, 60.0f);
            }
        }
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

    public void PathfindTo(Room target)
    {
        if (target == null) { return; }
        List<Cell> board = new List<Cell>();
        for (int i = 0; i < _dungeon.Board.Count; i++)
            board.Add(new Cell());

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        VisitNode(_start, target, path, solution, 0);

        OnDone?.Invoke(this);
        Debug.Log($"Found solution with {solution.Count} steps");
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

    public void VisitNode(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs)
{
    currentRoom.Visit(this);
    if (currentRoom == target) { return; }

    var neighbours = _dungeon.GetRoomNeighbours(this, currentRoom);

    if (neighbours.Count == 0)
    {
        if (path.Count == 0) { return; }
        currentRoom = path.Pop();
    }
    else
    {
        path.Push(currentRoom);
        var rnd = new System.Random();
        int randomIndex = rnd.Next(0, neighbours.Count);
        Room nextRoom = neighbours[randomIndex];
        solution.Add(currentRoom);
        VisitNode(nextRoom, target, path, solution, runs);
    }
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
}
}
