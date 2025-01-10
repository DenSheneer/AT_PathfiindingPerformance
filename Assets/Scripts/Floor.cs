using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class Floor : MonoBehaviour
{
    GameObject[] _tiles = null;
    public List<MeshRenderer> FloorMeshes = new List<MeshRenderer>();

    public void Initilize(GameObject[] tiles)
    {
        _tiles = tiles;
    }

    public GameObject GetMiddleTile()
    {
        var middle = _tiles[Mathf.FloorToInt(_tiles.Length * 0.5f)];
        return middle;
    }
    public void SetPathMaterial(Material material)
    {
        foreach (var tile in FloorMeshes)
        {
            tile.material = material;
        }
    }
}
