using System.Collections.Generic;
using MyBox;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Linq;
using System.Collections;
using Unity.VisualScripting;

public class MyAgent : MonoBehaviour
{
    [SerializeField] DungeonGenerator _dungeon;
    [SerializeField] Room _target = null;

    [Header("Materials")]
    [SerializeField] private Material _multiMaterial;
    [SerializeField] private Material _normalMaterial;
    [SerializeField] private Material _asyncMaterial;
    [SerializeField] private Material _aStarMaterial;

    Task _pathfindLoopTask;
    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    private Room _start;
    public UnityAction<MyAgent> OnDone;
    List<Room> _currentPath = null;
    Material _currentMaterial = null;

    public void Start()
    {

        SuperClass.Instance.DrawPathButtonClicked.AddListener(() => { drawPath(); });

        _dungeon.OnDungeonGenerated += (deepestRoom) =>
        {
            _target = deepestRoom;
            _start = _dungeon.RoomOnBoard[new Vector2Int(0, 0)];
            transform.position = _start.MiddlePosition();
        };
    }
    public void TestNoAsync()
    {
        _currentPath = PathfindTo(_target);
        _currentMaterial = _normalMaterial;
    }

    public async Task TestAsync()
    {
        _currentPath = await PathfindToAsync(_target);
        _currentMaterial = _asyncMaterial;
    }

    public async Task TestMultithread()
    {
        _currentPath = await Task.Run(() => PathfindToMultithread(_target));
        _currentMaterial = _multiMaterial;
    }

    public void TestAStar()
    {
        _currentPath = PathfindAStar();
        _currentMaterial = _aStarMaterial;
    }

    private void drawPath()
    {
        if (_currentPath != null)
        {
            foreach (var room in _currentPath)
            {
                room.SetFloorMaterial(_currentMaterial);
            }
            _currentPath[0].SetFloorMaterial(_normalMaterial);
            _currentPath[_currentPath.Count - 1].SetFloorMaterial(_normalMaterial);
        }
    }
    private void erasePath(List<Room> path)
    {
        if (path != null)
        {
            foreach (var room in path)
            {
                room.RestoreDefaultFloor();
            }
        }
    }

    public List<Room> PathfindTo(Room target)
    {
        if (target == null) { return null; }
        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();

        VisitNode(_start, target, path, solution, 0);

        OnDone?.Invoke(this);
        return solution;
    }

    public async Task<List<Room>> PathfindToMultithread(Room target)
    {
        if (target == null) { Debug.Log("null!"); return null; }
        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        //await Task.Run(() => VisitNodeMultithread(_start, target, path, solution, 0));
        await VisitNodeMultithread(_start, target, path, solution, 0);

        OnDone?.Invoke(this);

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

    public async void RepeatPathFind(EPathFindMode mode)
    {

        StopRepeatPathfind();

        switch (mode)
        {
            case EPathFindMode.DFS:
                _pathfindLoopTask = randomLoopAlgorithm(TestNoAsync, cancellationTokenSource.Token);
                break;
            case EPathFindMode.AsyncDFS:
                _pathfindLoopTask = asyncRandomLoopAlgorithm(TestAsync, cancellationTokenSource.Token);
                break;
            case EPathFindMode.MT_DFS:
                _pathfindLoopTask = asyncRandomLoopAlgorithm(TestMultithread, cancellationTokenSource.Token);

                break;
            case EPathFindMode.Astar:
                _pathfindLoopTask = randomLoopAlgorithm(TestAStar, cancellationTokenSource.Token);
                break;

        }
        await _pathfindLoopTask;
    }

    private async Task randomLoopAlgorithm(Action pathFindMethod, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            erasePath(_currentPath);

            _target = _dungeon.RoomOnBoard.Values.ToArray()[SuperClass.Instance.Random.Next(0, _dungeon.RoomOnBoard.Count)];
            pathFindMethod();

            if (SuperClass.Instance.AutoDraw)
                drawPath();

            _start = _target;
            try
            {
                await Task.Delay(SuperClass.Instance.PathfindCooldownMS, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    public async Task asyncRandomLoopAlgorithm(Func<Task> awaitablePathfindMethod, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            erasePath(_currentPath);

            _target = _dungeon.RoomOnBoard.Values.ToArray()[SuperClass.Instance.Random.Next(0, _dungeon.RoomOnBoard.Count)];
            await awaitablePathfindMethod();

            if (SuperClass.Instance.AutoDraw)
                drawPath();

            _start = _target;
            try
            {
                await Task.Delay(SuperClass.Instance.PathfindCooldownMS, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
    public void StopRepeatPathfind()
    {
        erasePath(_currentPath);

        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
        if (_pathfindLoopTask != null)
        {
            _pathfindLoopTask.Dispose();
        }

        cancellationTokenSource = new CancellationTokenSource();
        _pathfindLoopTask = null;
    }
}


