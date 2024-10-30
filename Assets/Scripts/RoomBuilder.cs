using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomBuilder : MonoBehaviour
{
    [SerializeField] BuilderSettings _settings;
    public Room CreateRoom(Vector3 position, int width = 1, int length = 1, Transform parent = null)
    {

        GameObject roomGO = new GameObject("Room");
        GameObject floorGO = new GameObject("Floor");
        GameObject[] wallsegmentsDown = new GameObject[width];
        GameObject[] wallsegmentsLeft = new GameObject[length];
        GameObject[] wallsegmentsUp = new GameObject[width];
        GameObject[] wallsegmentsRight = new GameObject[length];

        Room room = roomGO.AddComponent<Room>();

        if (_settings.FloorPrefabs.Length < 1 || _settings.WallPrefabs.Length < 1) { return null; }
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                int randomIndex = Random.Range(0, _settings.FloorPrefabs.Length);
                var floorX = GameObject.Instantiate(_settings.FloorPrefabs[randomIndex]);
                floorX.transform.position = new Vector3(_settings.offsetBetweenAssets * x, 0, 0);
                floorX.transform.parent = floorGO.transform;

                Vector3 floorZPosition = new Vector3(_settings.offsetBetweenAssets * x, 0, _settings.offsetBetweenAssets * z);
                int randomIndex2 = Random.Range(0, _settings.FloorPrefabs.Length);
                var floorZ = GameObject.Instantiate(_settings.FloorPrefabs[randomIndex2]);
                floorZ.transform.position = floorZPosition;
                floorZ.transform.parent = floorGO.transform;



                if (x == 0)
                    wallsegmentsLeft[z] = (makeWallSegment(floorZPosition + new Vector3(-2.17f, 0, 0), Quaternion.identity));
                if (x == width - 1)
                    wallsegmentsRight[z] = (makeWallSegment(floorZPosition + new Vector3(2.17f, 0, 0), Quaternion.identity));
                if (z == 0)
                    wallsegmentsDown[x] = makeWallSegment(floorZPosition + new Vector3(0, 0, -2.17f), Quaternion.Euler(0, 90.0f, 0));
                if (z == length - 1)
                    wallsegmentsUp[x] = makeWallSegment(floorZPosition + new Vector3(0, 0, 2.17f), Quaternion.Euler(0, 90.0f, 0));
            }
        }

        Wall[] walls = new Wall[4];
        walls[0] = makeWall(wallsegmentsUp, "WallUp");
        walls[1] = makeWall(wallsegmentsDown, "WallDown");
        walls[2] = makeWall(wallsegmentsRight, "WallRight");
        walls[3] = makeWall(wallsegmentsLeft, "WallLeft");

        room.Initialize(transform, walls, floorGO);
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
