using NUnit.Framework;
using System.Collections.Generic;
using MyBox;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;
using System.IO;

public class MyAgent : MonoBehaviour
{
    [SerializeField] DungeonGenerator _dungeon;
    [SerializeField] Room _target;

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
    public void TestAsync()
    {
        PathfindTo(_target);
    }

    public async void PathfindTo(Room target)
    {
        List<Cell> board = new List<Cell>();
        for (int i = 0; i < _dungeon.Board.Count; i++)
            board.Add(new Cell());

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        await VisitNode(_start, target, path, solution, 0);

        foreach (var room in solution)
            Debug.Log(room.BoardPosition);

        OnDone?.Invoke(this);
        Debug.Log($"Found solution with {solution.Count} steps");
    }

    public async Task VisitNode(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs)
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
        }
        Room nextRoom = neighbours[Random.Range(0, neighbours.Count)];
        solution.Add(currentRoom);
        await VisitNode(nextRoom, target, path, solution, runs);
    }
}
