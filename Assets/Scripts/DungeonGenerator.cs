using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon settings")]
    [SerializeField] RoomBuilder builder;
    [SerializeField] BuilderSettings _settings;

    private Vector2Int _size;
    private Vector2 _offset;

    [Header("Room settings")]
    [SerializeField] private int _roomWidth = 1;
    [SerializeField] private int _roomLength = 1;

    [SerializeField] private GameObject _loadingIndicator;

    int _startPos = 0;
    List<Cell> _board;
    Dictionary<Vector2Int, Room> _roomsOnBoard = new Dictionary<Vector2Int, Room>();

    public Action OnDungeonGenerated;
    public Action OnDungeonGenerateStart;

    public Dictionary<Vector2Int, Room> RoomOnBoard { get { return _roomsOnBoard; } }

    public List<Cell> Board { get { return _board; } }

    void Awake()
    {
        _offset = new Vector2(_settings.offsetBetweenAssets * _roomWidth, _settings.offsetBetweenAssets * _roomLength);
    }

    public void Reload()
    {
        if (_board != null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }

    }

    public async void GenerateMaze()
    {
        OnDungeonGenerateStart?.Invoke();
        _size.x = SuperClass.Instance.SizeX;
        _size.y = SuperClass.Instance.SizeY;

        _loadingIndicator.SetActive(true);
        await Task.Run(() => MazeGenerator());
        await createDungeon();
        _loadingIndicator.SetActive(false);
    }

    public void MazeGenerator()
    {
        _board = new List<Cell>();
        for (int x = 0; x < _size.x; x++)
        {
            for (int y = 0; y < _size.y; y++)
            {
                _board.Add(new Cell());
            }
        }

        int currentCellIndex = _startPos;
        Stack<int> path = new Stack<int>();

        int k = 0;


        while (true)   // HUGE!
        {
            k++;
            _board[currentCellIndex].isVisited = true;
            if (currentCellIndex == _board.Count - 1)
            {
                break;
            }

            List<int> neighbours = getCellNeighbours(currentCellIndex);

            if (neighbours.Count == 0)
            {
                if (path.Count == 0) { break; }

                currentCellIndex = path.Pop();
            }
            else
            {
                path.Push(currentCellIndex);

                int newCell = neighbours[SuperClass.Instance.Random.Next(0, neighbours.Count)];
                if (newCell > currentCellIndex)
                {
                    // down or right
                    if (newCell - 1 == currentCellIndex)
                    {
                        // right
                        _board[currentCellIndex].status[2] = true;
                        currentCellIndex = newCell;
                        _board[currentCellIndex].status[3] = true;
                    }
                    else
                    {
                        // down
                        _board[currentCellIndex].status[1] = true;
                        currentCellIndex = newCell;
                        _board[currentCellIndex].status[0] = true;
                    }
                }
                else
                {
                    // up or left
                    if (newCell + 1 == currentCellIndex)
                    {
                        // left
                        _board[currentCellIndex].status[3] = true;
                        currentCellIndex = newCell;
                        _board[currentCellIndex].status[2] = true;
                    }
                    else
                    {
                        // up
                        _board[currentCellIndex].status[0] = true;
                        currentCellIndex = newCell;
                        _board[currentCellIndex].status[1] = true;
                    }
                }
            }
        }
    }

    async Task createDungeon()
    {
        int yield = 0;
        for (int x = 0; x < _size.x; x++)
        {
            for (int y = 0; y < _size.y; y++)
            {
                yield++;

                Cell currentcell = _board[x + y * _size.x];
                if (currentcell.isVisited)
                {
                    var boardPosition = new Vector2Int(x, y);
                    var newRoom = builder.CreateRoom(new Vector3(x * _offset.x, 0, -y * _offset.y), boardPosition, _roomWidth, _roomLength, transform);
                    newRoom.UpdateRoom(currentcell.status);
                    _roomsOnBoard.Add(boardPosition, newRoom);

                    if (yield % 50 == 0)
                        await Task.Yield();
                }
            }
        }
        OnDungeonGenerated?.Invoke();

        for (int i = 0; i < Mathf.Abs(_roomsOnBoard.Count * 0.1f); i++)  //  open all doors of 10% of the rooms
            openRandomRoomDoors();

    }
    public Vector2 RealSize { get { return new Vector2((_size.x + 2) * _offset.x, (_size.y - 2) * _offset.y); } }
    public int MaxSize { get { return _size.x * _size.y; } }



    private void openRandomRoomDoors()
    {
        var randomRoom = _roomsOnBoard.Values.ToArray()[SuperClass.Instance.Random.Next(0, _roomsOnBoard.Count)];

        bool[] allDoorsOpen = { true, true, true, true };
        randomRoom.Doors = allDoorsOpen;

        var neighbours = GetRoomNeighbours(randomRoom);

        if (neighbours[0] != null)
        {
            neighbours[0].Doors[1] = true;
            neighbours[0].UpdateRoom(neighbours[0].Doors);
        }
        else { allDoorsOpen[0] = false; }

        if (neighbours[1] != null)
        {
            var downdoors = neighbours[1].Doors[0] = true;
            neighbours[1].UpdateRoom(neighbours[1].Doors);
        }
        else { allDoorsOpen[1] = false; }

        if (neighbours[2] != null)
        {
            neighbours[2].Doors[3] = true;
            neighbours[2].UpdateRoom(neighbours[2].Doors);
        }
        else { allDoorsOpen[2] = false; }

        if (neighbours[3] != null)
        {
            neighbours[3].Doors[2] = true;
            neighbours[3].UpdateRoom(neighbours[3].Doors);
        }
        else { allDoorsOpen[3] = false; }

        randomRoom.UpdateRoom(allDoorsOpen);
    }

    public List<Room> GetRoomNeighbours(Room room)
    {
        List<Room> roomNeighbours = new List<Room>();

        if (room.Doors[0]) // up
        {
            Room up = null;
            _roomsOnBoard.TryGetValue(room.BoardPosition + new Vector2Int(0, -1), out up);
            roomNeighbours.Add(up);
        }

        if (room.Doors[1])  // down
        {
            Room down = null;
            _roomsOnBoard.TryGetValue(room.BoardPosition + new Vector2Int(0, 1), out down);
            roomNeighbours.Add(down);
        }

        if (room.Doors[2]) // right
        {
            Room right = null;
            _roomsOnBoard.TryGetValue(room.BoardPosition + new Vector2Int(1, 0), out right);
            roomNeighbours.Add(right);
        }

        if (room.Doors[3]) // left
        {
            Room left = null;
            _roomsOnBoard.TryGetValue(room.BoardPosition + new Vector2Int(-1, 0), out left);
            roomNeighbours.Add(left);
        }

        return roomNeighbours;
    }

    public List<Room> GetRoomNeighbours(PathfindAgent agent, Room room)
    {
        List<Room> roomNeighbours = new List<Room>();

        if (room.Doors[0]) // up
        {
            var up = _roomsOnBoard[room.BoardPosition + new Vector2Int(0, -1)];
            if (!up.HasBeenVisitedBy(agent))
                roomNeighbours.Add(up);
        }

        if (room.Doors[1])  // down
        {
            var down = _roomsOnBoard[room.BoardPosition + new Vector2Int(0, 1)];
            if (!down.HasBeenVisitedBy(agent))
                roomNeighbours.Add(down);
        }

        if (room.Doors[2]) // right
        {
            var right = _roomsOnBoard[room.BoardPosition + new Vector2Int(1, 0)];
            if (!right.HasBeenVisitedBy(agent))
                roomNeighbours.Add(right);
        }

        if (room.Doors[3]) // left
        {
            var left = _roomsOnBoard[room.BoardPosition + new Vector2Int(-1, 0)];
            if (!left.HasBeenVisitedBy(agent))
                roomNeighbours.Add(left);
        }

        return roomNeighbours;
    }

    List<int> getCellNeighbours(int cell)
    {
        List<int> neighbours = new List<int>();

        //      up
        int upNeighbourPos = cell - _size.x;
        if (cell - _size.x >= 0 && !_board[upNeighbourPos].isVisited)
        {
            neighbours.Add(upNeighbourPos);
        }

        //      down
        int downNeighbourPos = cell + _size.x;
        if (cell + _size.x < _board.Count && !_board[downNeighbourPos].isVisited)
        {
            neighbours.Add(downNeighbourPos);
        }

        //      right
        int rightNeighbourPos = cell + 1;
        if ((cell + 1) % _size.x != 0 && !_board[rightNeighbourPos].isVisited)
        {
            neighbours.Add(rightNeighbourPos);
        }

        //      left
        int leftNeighbourPos = cell - 1;
        if (cell % _size.x != 0 && !_board[leftNeighbourPos].isVisited)
        {
            neighbours.Add(leftNeighbourPos);
        }


        return neighbours;
    }
}
public class Cell
{
    public bool isVisited = false;
    public bool[] status = new bool[4];
}