using NUnit.Framework;
using System.Collections.Generic;
using MyBox;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;

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
    public void Test()
    {
        PathfindTo(_target);
    }

    public void PathfindTo(Room target)
    {
        List<Cell> board = new List<Cell>();
        for (int i = 0; i < _dungeon.Board.Count; i++)
            board.Add(new Cell());

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();
        Room currentRoom = _start;

        int k = 0;
        while (k < 1000)
        {
            k++;

            currentRoom.Visit(this);
            if (currentRoom == target) { break; }

            var neighbours = _dungeon.GetRoomNeighbours(this, currentRoom);
            if (neighbours.Count == 0)
            {
                if (path.Count == 0) { break; }
                currentRoom = path.Pop();
            }
            else
            {
                path.Push(currentRoom);
                currentRoom = neighbours[Random.Range(0, neighbours.Count)];

            }
            solution.Add(currentRoom);
        }
        Debug.Log($"Found solution with {solution.Count} steps");

        foreach (var room in solution)
            Debug.Log(room.BoardPosition);

        _start = target;
        OnDone?.Invoke(this);
    }
}
