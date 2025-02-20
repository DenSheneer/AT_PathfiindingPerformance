using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomBuilder : MonoBehaviour
{
    [SerializeField] BuilderSettings _settings;
    [SerializeField] Room _roomPrefab;
    Vector2Int _position;
    public Room CreateRoom(Vector3 position, Vector2Int boardPosition, int width = 1, int length = 1, Transform parent = null)
    {
        Room room = Instantiate<Room>(_roomPrefab);
        room.name = "Room";

        GameObject floorGO = new GameObject("Floor");
        GameObject[] wallsegmentsUp = new GameObject[width];
        GameObject[] wallsegmentsRight = new GameObject[length];
        GameObject[] wallsegmentsDown = new GameObject[width];
        GameObject[] wallsegmentsLeft = new GameObject[length];

        Floor floor = floorGO.AddComponent<Floor>();


        GameObject[] tiles = new GameObject[width * length];
        int i = 0;
        if (_settings.FloorPrefabs.Length < 1 || _settings.WallPrefabs.Length < 1) { return null; }
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                Vector3 floorPos = new Vector3(_settings.offsetBetweenAssets * x, 0, _settings.offsetBetweenAssets * z);
                int randomGFXIndex = Random.Range(0, _settings.FloorPrefabs.Length);
                var tileGO = GameObject.Instantiate(_settings.FloorPrefabs[randomGFXIndex], floor.transform);
                tileGO.name = $"Tile {i}";
                tileGO.transform.position = floorPos;
                tiles[i] = tileGO;

                floor.Initilize(tiles);


                if (x == 0)
                    wallsegmentsLeft[z] = (makeWallSegment(floorPos + new Vector3(-2.17f, 0, 0), Quaternion.identity));
                if (x == width - 1)
                    wallsegmentsRight[z] = (makeWallSegment(floorPos + new Vector3(2.17f, 0, 0), Quaternion.identity));
                if (z == 0)
                    wallsegmentsDown[x] = makeWallSegment(floorPos + new Vector3(0, 0, -2.17f), Quaternion.Euler(0, 90.0f, 0));
                if (z == length - 1)
                    wallsegmentsUp[x] = makeWallSegment(floorPos + new Vector3(0, 0, 2.17f), Quaternion.Euler(0, 90.0f, 0));

                i++;
            }
        }
        List<MeshRenderer> renderers = new List<MeshRenderer>(); // To set the material of the found path later
        foreach (var tile in tiles)
        {
            renderers.Add(tile.GetComponentInChildren<MeshRenderer>());
        }
        floor.FloorMeshes = renderers;

        Wall[] walls = new Wall[4];
        walls[0] = makeWall(wallsegmentsUp, "WallUp");
        walls[1] = makeWall(wallsegmentsDown, "WallDown");
        walls[2] = makeWall(wallsegmentsRight, "WallRight");
        walls[3] = makeWall(wallsegmentsLeft, "WallLeft");

        room.Initialize(transform, walls, floor, boardPosition);
        room.name = $"Room {boardPosition}";
        room.transform.position = position;
        return room;
    }

    private Wall makeWall(GameObject[] segments, string name)
    {
        GameObject wallGO = new GameObject(name);
        Wall wall = wallGO.AddComponent<Wall>();
        wall.name = name;
        wall.Initialize(segments, _settings);

        return wall;
    }

    private GameObject makeWallSegment(Vector3 position, Quaternion rotation)
    {
        var segment = Instantiate(_settings.WallPrefabs[Random.Range(0, _settings.WallPrefabs.Length)]);
        segment.transform.SetPositionAndRotation(position, rotation);
        return segment;
    }
}
