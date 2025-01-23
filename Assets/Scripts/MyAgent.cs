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
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TMPro;

public class MyAgent : MonoBehaviour
{
    [SerializeField] DungeonGenerator _dungeon;
    [SerializeField] Room _target = null;
    [SerializeField] private TMP_Text _textRuns;

    [Header("Materials")]
    [SerializeField] private Material _multiMaterial;
    [SerializeField] private Material _normalMaterial;
    [SerializeField] private Material _asyncMaterial;
    [SerializeField] private Material _aStarMaterial;

    Task _pathfindLoopTask;
    CancellationTokenSource cancellationTokenSource = null;
    PathfindingJsonWriter writer = new PathfindingJsonWriter();

    private Room _start;
    public UnityAction<MyAgent> OnDone;
    List<Room> _currentPath = null;
    Material _currentMaterial = null;
    private EPathFindMode _currentpathFindmode;
    private List<PathfindResult> _results = new List<PathfindResult>();

    int _runs = 0;

    public void Start()
    {

        SuperClass.Instance.DrawPathButtonClicked.AddListener(() => { drawPath(_currentPath); });

        _dungeon.OnDungeonGenerated += (deepestRoom) =>
        {
            _target = deepestRoom;
            _start = _dungeon.RoomOnBoard[new Vector2Int(0, 0)];
            transform.position = _start.MiddlePosition();
        };
    }

    private void drawPath(List<Room> path)
    {
        if (path != null)
        {
            foreach (var room in path)
            {
                room.SetFloorMaterial(_currentMaterial);
            }
            path[0].SetFloorMaterial(_normalMaterial);
            path[_currentPath.Count - 1].SetFloorMaterial(_normalMaterial);
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
    public List<Room> PathfindTo()
    {
        if (_target == null) { return null; }
        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();

        VisitNode(_start, _target, path, solution, 0);

        OnDone?.Invoke(this);
        return solution;
    }

    public async Task<List<Room>> PathfindToMultithread(CancellationToken cancellationToken)
    {
        if (_target == null) { return null; }

        cancellationToken.ThrowIfCancellationRequested();

        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();

           
        await Task.Run(() => VisitNodeMultithread(_start, _target, path, solution, 0, cancellationToken));

        OnDone?.Invoke(this);

        return solution;
    }

    public async Task<List<Room>> PathfindToAsync(CancellationToken cancellationToken)
    {
        if (_target == null) { return null; }

        cancellationToken.ThrowIfCancellationRequested();
        List<Room> solution = new List<Room>();
        Stack<Room> path = new Stack<Room>();

        await VisitNodeAsync(_start, _target, path, solution, 0, cancellationToken);

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

    public async void RepeatPathFind(EPathFindMode mode)
    {

        StopRepeatPathfind();
        _currentpathFindmode = mode;
        _runs = 0;

        switch (mode)
        {
            case EPathFindMode.DFS:
                _pathfindLoopTask = randomLoopAlgorithm(PathfindTo, cancellationTokenSource.Token);
                _currentMaterial = _normalMaterial;
                break;
            case EPathFindMode.AsyncDFS:
                _currentMaterial = _asyncMaterial;
                _pathfindLoopTask = asyncRandomLoopAlgorithm(PathfindToAsync, cancellationTokenSource.Token);
                break;
            case EPathFindMode.MT_DFS:
                _currentMaterial = _multiMaterial;

                _pathfindLoopTask = asyncRandomLoopAlgorithm(PathfindToMultithread, cancellationTokenSource.Token);

                break;
            case EPathFindMode.Astar:
                _currentMaterial = _aStarMaterial;
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

            if (SuperClass.Instance.AutoDraw)
                drawPath(_currentPath);

            _runs++;
            _textRuns.text = _runs.ToString();

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
            Debug.Log(_target.name);

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

            if (SuperClass.Instance.AutoDraw)
                drawPath(_currentPath);

            _runs++;
            _textRuns.text = _runs.ToString();

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
        Debug.Log("saved to: " + Application.persistentDataPath);

        // Output file will be saved in the executable's working directory
    }
}


