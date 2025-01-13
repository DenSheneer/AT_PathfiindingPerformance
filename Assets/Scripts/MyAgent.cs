using System.Collections.Generic;
using MyBox;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using System;

public class MyAgent : MonoBehaviour
{
    [SerializeField] DungeonGenerator _dungeon;
    [SerializeField] Room _target = null;

    [Header("Materials")]
    [SerializeField] private Material _multiMaterial;
    [SerializeField] private Material _normalMaterial;
    [SerializeField] private Material _asyncMaterial;
    [SerializeField] private Material _aStarMaterial;

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

        drawPath(path, _normalMaterial);
    }

    [ButtonMethod]
    public async void TestAsync()
    {
        var timeNow = DateTime.Now;
        var path = await PathfindToAsync(_target);
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("Async duration in milliseconds: " + duration.Milliseconds);

        drawPath(path, _asyncMaterial);
    }

    [ButtonMethod]
    public async void TestMultithread()
    {
        var timeNow = DateTime.Now;
        var path = await Task.Run(() => PathfindToMultithread(_target));
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log("Multithread duration in milliseconds: " + duration.Milliseconds);

        drawPath(path, _multiMaterial);
    }

    [ButtonMethod]
    public void TestAStar()
    {
        var timeNow = DateTime.Now;
        var path = PathfindAStar();
        var timeAfter = DateTime.Now;

        var duration = timeAfter.Subtract(timeNow);
        Debug.Log($"A*: found solution with {path.Count} steps in {duration.Milliseconds} ms");
        drawPath(path, _aStarMaterial);
    }

    private void drawPath(List<Room> path, Material material)
    {
        if (path != null)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                path[i].SetFloorMaterial(material);
                //Debug.DrawLine(path[i].MiddlePosition(), path[i + 1].MiddlePosition(), UnityEngine.Color.green, 60.0f);
            }
            path[0].SetFloorMaterial(_normalMaterial);
            path[path.Count - 1].SetFloorMaterial(_normalMaterial);
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
            Heap<RoomData> openSet = new Heap<RoomData>(_dungeon.MaxSize);
            HashSet<Room> closedSet = new HashSet<Room>();

            var startRoomData = new RoomData(_start);
            _start.MakeNewRoomData(this, startRoomData);
            openSet.Add(startRoomData);

            while (openSet.Count > 0)
            {
                var currentRoom = openSet.RemoveFirst();

                closedSet.Add(currentRoom.roomObject);
                if (currentRoom.roomObject == _target)
                {
                    return retracePath(_start, _target);
                }

                foreach (var neighbour in _dungeon.GetRoomNeighbours(currentRoom.roomObject))
                {
                    if (closedSet.Contains(neighbour)) { continue; }

                    int newMoveCostToNeighbour = currentRoom.gCost + getDistance(currentRoom.roomObject, neighbour);

                    RoomData neighbourRoomData = null;

                    if (!neighbour.pathfindingData.ContainsKey(this))
                    {
                        neighbour.MakeNewRoomData(this, new RoomData(neighbour));
                    }

                    neighbourRoomData = neighbour.pathfindingData[this];

                    if (newMoveCostToNeighbour < neighbourRoomData.gCost ||
                        !openSet.Contains(neighbourRoomData)
                        )
                    {
                        neighbourRoomData.gCost = newMoveCostToNeighbour;
                        neighbourRoomData.hCost = getDistance(neighbour, _target);
                        neighbourRoomData.parent = currentRoom;

                        if (!openSet.Contains(neighbour.pathfindingData[this]))
                        {
                            openSet.Add(neighbourRoomData);
                        }
                    }
                }
            }
        }
        OnDone?.Invoke(this);
        return null;
    }

    private List<Room> retracePath(Room start, Room end)
    {
        List<Room> path = new List<Room>();
        Room currentRoom = end;

        while (currentRoom != start)
        {
            path.Add(currentRoom);
            currentRoom = currentRoom.pathfindingData[this].parent.roomObject;
        }
        path.Add(_start);
        path.Reverse();

        OnDone?.Invoke(this);

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
