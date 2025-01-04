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
    public void TestAsync()
    {
        var timeNow = DateTime.Now;
        PathfindTo(_target);
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("Async duration in milliseconds: " + duration.Milliseconds);
    }

    [ButtonMethod]
    public async void TestMultithread()
    {
        var timeNow = DateTime.Now;
        await Task.Run(() => PathfindToAsync(_target));
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("Multithread duration in milliseconds: " + duration.Milliseconds);
    }


    public async Task PathfindToMultithread(Room target)
    {
        if (target == null) { return; }
        List<Cell> board = new List<Cell>();
        for (int i = 0; i < _dungeon.Board.Count; i++)
            board.Add(new Cell());

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        await Task.Run(() => VisitNodeMultithread(_start, target, path, solution, 0));


        OnDone?.Invoke(this);
        Debug.Log($"Found solution with {solution.Count} steps");
    }

    public async void PathfindToAsync(Room target)
    {
        if (target == null) { return; }
        List<Cell> board = new List<Cell>();
        for (int i = 0; i < _dungeon.Board.Count; i++)
            board.Add(new Cell());

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        await VisitNodeAsync(_start, target, path, solution, 0);

        OnDone?.Invoke(this);
        Debug.Log($"Found solution with {solution.Count} steps");
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

    public async Task VisitNodeMultithread(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs)
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
            await Task.Run(() => VisitNodeMultithread(nextRoom, target, path, solution, runs));
            //await VisitNodeMultithread(nextRoom, target, path, solution, runs);
        }
    }

    public async Task VisitNodeAsync(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs)
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
            await VisitNodeAsync(nextRoom, target, path, solution, runs);
        }
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
