using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public class Cell
    {
        public bool isVisited = false;
        public bool[] status = new bool[4];
    }

    [Header("Dungeon settings")]
    [SerializeField] RoomBuilder builder;
    [SerializeField] BuilderSettings _settings;
    [SerializeField] Vector2Int _size;
    Vector2 _offset;

    [Header("Room settings")]
    [SerializeField] int _roomWidth = 1;
    [SerializeField] int _roomLength = 1;

    int startPos = 0;
    List<Cell> board;

    void Start()
    {
        _offset = new Vector2(_settings.offsetBetweenAssets * _roomWidth, _settings.offsetBetweenAssets * _roomLength);
        mazeGenerator();
    }

    public void CreateDungeon()
    {
        for (int x = 0; x < _size.x; x++)
        {
            for (int y = 0; y < _size.y; y++)
            {
                Cell currentcell = board[x + y * _size.x];
                if (currentcell.isVisited)
                {
                    var newRoom = builder.CreateRoom(new Vector3(x * _offset.x, 0, -y * _offset.y), _roomWidth, _roomLength, transform);
                    newRoom.UpdateRoom(currentcell.status);
                    newRoom.name = $"Room {x}, {y}";
                }
            }
        }
    }

    void mazeGenerator()
    {
        board = new List<Cell>();
        for (int x = 0; x < _size.x; x++)
        {
            for (int y = 0; y < _size.y; y++)
            {
                board.Add(new Cell());
            }
        }

        int currentCellIndex = startPos;
        Stack<int> path = new Stack<int>();

        int k = 0;

        while (k < 1000)
        {
            k++;
            board[currentCellIndex].isVisited = true;
            if (currentCellIndex == board.Count - 1)
            {
                break;
            }

            List<int> neighbours = checkNeighbours(currentCellIndex);

            if (neighbours.Count == 0)
            {
                if (path.Count == 0) { break; }

                currentCellIndex = path.Pop();
            }
            else
            {
                path.Push(currentCellIndex);

                int newCell = neighbours[Random.Range(0, neighbours.Count)];
                if (newCell > currentCellIndex)
                {
                    // down or right
                    if (newCell - 1 == currentCellIndex)
                    {
                        // right
                        board[currentCellIndex].status[2] = true;
                        currentCellIndex = newCell;
                        board[currentCellIndex].status[3] = true;
                    }
                    else
                    {
                        // down
                        board[currentCellIndex].status[1] = true;
                        currentCellIndex = newCell;
                        board[currentCellIndex].status[0] = true;
                    }
                }
                else
                {
                    // up or left
                    if (newCell + 1 == currentCellIndex)
                    {
                        // left
                        board[currentCellIndex].status[3] = true;
                        currentCellIndex = newCell;
                        board[currentCellIndex].status[2] = true;
                    }
                    else
                    {
                        // up
                        board[currentCellIndex].status[0] = true;
                        currentCellIndex = newCell;
                        board[currentCellIndex].status[1] = true;
                    }
                }
            }
        }
        CreateDungeon();
    }
    List<int> checkNeighbours(int cell)
    {
        List<int> neighbours = new List<int>();

        //      up
        int upNeighbourPos = cell - _size.x;
        if (cell - _size.x >= 0 && !board[upNeighbourPos].isVisited)
        {
            neighbours.Add(upNeighbourPos);
        }

        //      down
        int downNeighbourPos = cell + _size.x;
        if (cell + _size.x < board.Count && !board[downNeighbourPos].isVisited)
        {
            neighbours.Add(downNeighbourPos);
        }

        //      right
        int rightNeighbourPos = cell + 1;
        if ((cell + 1) % _size.x != 0 && !board[rightNeighbourPos].isVisited)
        {
            neighbours.Add(rightNeighbourPos);
        }

        //      left
        int leftNeighbourPos = cell - 1;
        if (cell % _size.x != 0 && !board[leftNeighbourPos].isVisited)
        {
            neighbours.Add(leftNeighbourPos);
        }


        return neighbours;
    }
}