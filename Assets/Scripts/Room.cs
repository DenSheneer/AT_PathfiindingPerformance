using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    BuilderSettings _settings;
    GameObject _floor;
    Wall[] _walls = new Wall[4];
    private bool[] openDoors = { false, false, false, false };

    public void Initialize(Transform parent, Wall[] walls, GameObject floor)
    {
        transform.parent = parent;

        _floor = floor;
        _floor.transform.parent = transform;

        _walls = walls;
        foreach (Wall wall in _walls)
        {
            wall.transform.parent = transform;
        }
    }

    public void UpdateRoom(bool[] newOpenDoors)
    {
        for (int i = 0; i < _walls.Length; i++)
            if (newOpenDoors[i]) { _walls[i].MakeDoorInMiddle(); }
    }
}
