using System.Collections.Generic;
using MyBox;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class PathfindAgent : MonoBehaviour
{
    [SerializeField] DungeonGenerator _dungeon;
    [SerializeField] Room _target = null;

    private Room _start;
    List<Room> _currentPath = null;

    Task _pathfindLoopTask;
    CancellationTokenSource cancellationTokenSource = null;
    PathfindingJsonWriter writer = new PathfindingJsonWriter();

    private EPathFindMode _currentpathFindmode;
    private List<PathfindResult> _results = new List<PathfindResult>();

    public UnityAction<PathfindAgent> OnDFS_Done;
    public Action<List<Room>, EPathFindMode> OnPathFound;

    public void Start()
    {
        _dungeon.OnDungeonGenerated += () =>
        {
            _start = _dungeon.RoomOnBoard[new Vector2Int(0, 0)];
            transform.position = _start.MiddlePosition();
        };
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
    public List<Room> DFS()
    {
        if (_target == null) { return null; }
        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();

        VisitNode(_start, _target, path, solution, 0);

        OnPathFound?.Invoke(solution, EPathFindMode.DFS);
        OnDFS_Done?.Invoke(this);
        return solution;
    }

    public async Task<List<Room>> DFS_Multi(CancellationToken cancellationToken)
    {
        if (_target == null) { return null; }

        cancellationToken.ThrowIfCancellationRequested();

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();


        await Task.Run(() => VisitNodeMultithread(_start, _target, path, solution, 0, cancellationToken));

        OnPathFound?.Invoke(solution, EPathFindMode.MT_DFS);
        OnDFS_Done?.Invoke(this);

        return solution;
    }

    public async Task<List<Room>> DFS_Async(CancellationToken cancellationToken)
    {
        if (_target == null) { return null; }

        cancellationToken.ThrowIfCancellationRequested();
        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();

        await VisitNodeAsync(_start, _target, path, solution, 0, cancellationToken);

        OnPathFound?.Invoke(solution, EPathFindMode.AsyncDFS);
        OnDFS_Done?.Invoke(this);
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

    public async Task<bool> VisitNodeAsync(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
            cancellationToken.ThrowIfCancellationRequested();

            if (await VisitNodeAsync(neighbor, target, path, solution, runs, cancellationToken))
            {
                solution.Add(currentRoom);
                return true;
            }
        }

        return false;
    }

    public async Task<bool> VisitNodeMultithread(Room currentRoom, Room target, Stack<Room> path, List<Room> solution, int runs, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
            cancellationToken.ThrowIfCancellationRequested();

            if (await Task.Run(() => VisitNodeMultithread(neighbor, target, path, solution, runs, cancellationToken)))
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
                    var solution = retracePath(_start, _target);
                    OnPathFound?.Invoke(solution, EPathFindMode.Astar);
                    return solution;
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

        OnDFS_Done?.Invoke(this);

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

    public async void RepeatPathFind(EPathFindMode mode)
    {

        StopRepeatPathfind();
        _currentpathFindmode = mode;

        switch (mode)
        {
            case EPathFindMode.DFS:
                _pathfindLoopTask = randomLoopAlgorithm(DFS, cancellationTokenSource.Token);
                break;
            case EPathFindMode.AsyncDFS:
                _pathfindLoopTask = asyncRandomLoopAlgorithm(DFS_Async, cancellationTokenSource.Token);
                break;
            case EPathFindMode.MT_DFS:

                _pathfindLoopTask = asyncRandomLoopAlgorithm(DFS_Multi, cancellationTokenSource.Token);

                break;
            case EPathFindMode.Astar:
                _pathfindLoopTask = randomLoopAlgorithm(PathfindAStar, cancellationTokenSource.Token);
                break;

        }
        await _pathfindLoopTask;
    }

    private async Task randomLoopAlgorithm(Func<List<Room>> pathFindMethod, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            erasePath(_currentPath);

            _target = _dungeon.RoomOnBoard.Values.ToArray()[SuperClass.Instance.Random.Next(0, _dungeon.RoomOnBoard.Count)];

            var stopwatch = Stopwatch.StartNew();
            _currentPath = pathFindMethod();
            stopwatch.Stop();

            var result = new PathfindResult(SuperClass.Instance.SizeX,
                                           SuperClass.Instance.SizeY,
                                           _dungeon.RoomOnBoard.Count,
                                           _currentpathFindmode,
                                           stopwatch.Elapsed.TotalMilliseconds,
                                           _currentPath.Count,
                                           SuperClass.Instance.RandomSeed
                                           );

            _results.Add(result);
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

    public async Task asyncRandomLoopAlgorithm(Func<CancellationToken, Task<List<Room>>> awaitablePathfindMethod, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            erasePath(_currentPath);

            _target = _dungeon.RoomOnBoard.Values.ToArray()[SuperClass.Instance.Random.Next(0, _dungeon.RoomOnBoard.Count)];

            var stopwatch = Stopwatch.StartNew();
            _currentPath = await awaitablePathfindMethod(cancellationToken);
            stopwatch.Stop();

            var result = new PathfindResult(SuperClass.Instance.SizeX,
                                           SuperClass.Instance.SizeY,
                                           _dungeon.RoomOnBoard.Count,
                                           _currentpathFindmode,
                                           stopwatch.Elapsed.TotalMilliseconds,
                                           _currentPath.Count,
                                           SuperClass.Instance.RandomSeed
                                           );

            _results.Add(result);
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

        // Write to JSON
        writer.AppendPathfindingResultToJson($"{Application.persistentDataPath}/PathfindingResult.json", _results);
    }
}


